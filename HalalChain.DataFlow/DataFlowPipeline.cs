using System.Diagnostics;
using HalalChain.DataFlow.Configuration;
using HalalChain.DataFlow.Destination;
using HalalChain.DataFlow.Models;
using HalalChain.DataFlow.Source;
using HalalChain.DataFlow.Transform;
using HalalChain.DataFlow.Validation;
using Microsoft.Extensions.Logging;

namespace HalalChain.DataFlow;

/// <summary>
/// Runs a source-to-destination pipeline: extract, transform, validate,
/// then load, advancing a checkpoint only after a batch commits. Batches
/// that exhaust their retries are dead-lettered and the checkpoint still
/// advances, so one poisoned row cannot stall the whole sync.
/// </summary>
public sealed class DataFlowPipeline<TRecord> : IDataFlowPipeline<TRecord>
    where TRecord : class, new()
{    private readonly IDataFlowSource<TRecord> _source;
    private readonly IDataFlowDestination<TRecord> _destination;
    private readonly DataFlowPipelineOptions _options;
    private readonly IDeadLetterSink _deadLetterSink;
    private readonly ILogger<DataFlowPipeline<TRecord>> _logger;
    private readonly IDataFlowValidator<TRecord>[] _validators;
    private readonly IDataFlowTransformer<TRecord, TRecord>[] _transformers;

    public DataFlowPipeline(
        IDataFlowSource<TRecord> source,
        IDataFlowDestination<TRecord> destination,
        DataFlowPipelineOptions options,
        IEnumerable<IDataFlowValidator<TRecord>> validators,
        IEnumerable<IDataFlowTransformer<TRecord, TRecord>> transformers,
        IDeadLetterSink deadLetterSink,
        ILogger<DataFlowPipeline<TRecord>> logger)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(deadLetterSink);
        ArgumentNullException.ThrowIfNull(logger);

        _source = source;
        _destination = destination;
        _options = options;
        _deadLetterSink = deadLetterSink;
        _logger = logger;
        _validators = validators?.ToArray() ?? [];
        _transformers = transformers?.ToArray() ?? [];
    }

    public string Name => _options.Name;

    public async Task<DataFlowMetrics> RunAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new DataFlowMetrics { PipelineName = _options.Name };
        var total = Stopwatch.StartNew();

        await _source.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _destination.InitializeAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Starting data flow pipeline {Pipeline}", _options.Name);

        try
        {
            await foreach (var _ in RunBatchesAsync(metrics, cancellationToken).ConfigureAwait(false))
            {
                // Draining the enumerable is the work; the item is unused.
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Data flow pipeline {Pipeline} was cancelled", _options.Name);
            throw;
        }

        total.Stop();
        metrics.TotalDuration = total.Elapsed;
        metrics.CompletedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Pipeline {Pipeline} finished: {Loaded} loaded, {Failed} failed, {Rate:F1}% success in {Duration}",
            _options.Name, metrics.TotalRecordsLoaded, metrics.TotalRecordsFailed,
            metrics.SuccessRate, metrics.TotalDuration);

        return metrics;
    }

    private async IAsyncEnumerable<DataFlowBatch<TRecord>> RunBatchesAsync(
        DataFlowMetrics metrics,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _source.HasMoreDataAsync(cancellationToken).ConfigureAwait(false))
        {
            var batch = await _source.ExtractBatchAsync(cancellationToken).ConfigureAwait(false);

            if (batch.RecordCount == 0)
            {
                yield return batch;
                break;
            }

            metrics.TotalRecordsExtracted += batch.RecordCount;
            metrics.TotalBatches++;

            var outcome = await ProcessBatchAsync(batch, metrics, cancellationToken).ConfigureAwait(false);

            // The checkpoint advances past both committed and dead-lettered
            // batches. Only an unrecoverable extraction failure should cause
            // the next run to re-read the same rows.
            await _source.SetCheckpointAsync(
                await _source.GetCheckpointAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            yield return outcome;
        }
    }

    private async Task<DataFlowBatch<TRecord>> ProcessBatchAsync(
        DataFlowBatch<TRecord> batch,
        DataFlowMetrics metrics,
        CancellationToken cancellationToken)
    {
        batch.Status = DataFlowBatchStatus.Transforming;

        if (_options.EnableTransformation)
        {
            batch.Records = ApplyTransformers(batch.Records);
            batch.Status = DataFlowBatchStatus.Transformed;
        }

        if (_options.EnableValidation)
        {
            batch.Status = DataFlowBatchStatus.Validating;
            await SeparateInvalidRecordsAsync(batch, metrics, cancellationToken).ConfigureAwait(false);
            batch.Status = DataFlowBatchStatus.Validated;
        }

        if (batch.RecordCount == 0)
        {
            batch.Status = DataFlowBatchStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;
            return batch;
        }

        batch.Status = DataFlowBatchStatus.Loading;
        var result = await LoadWithRetryAsync(batch, metrics, cancellationToken).ConfigureAwait(false);

        batch.Status = result.Status;
        batch.CompletedAt = DateTimeOffset.UtcNow;
        return batch;
    }

    private List<TRecord> ApplyTransformers(List<TRecord> records)
    {
        if (_transformers.Length == 0) return records;

        var transformed = new List<TRecord>(records.Count);

        foreach (var record in records)
        {
            var current = record;

            foreach (var transformer in _transformers)
            {
                try
                {
                    current = transformer.Transform(current);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transformer {Transformer} failed for a record; dropping it",
                        transformer.Name);
                    break;
                }
            }

            if (current is not null) transformed.Add(current);
        }

        return transformed;
    }

    private async Task SeparateInvalidRecordsAsync(
        DataFlowBatch<TRecord> batch,
        DataFlowMetrics metrics,
        CancellationToken cancellationToken)
    {
        if (_validators.Length == 0) return;

        var valid = new List<TRecord>(batch.RecordCount);
        var deadLetters = new List<DataFlowErrorRecord>();

        foreach (var record in batch.Records)
        {
            var errors = new List<ValidationError>();

            foreach (var validator in _validators)
            {
                var outcome = validator.Validate(record);
                errors.AddRange(outcome.Errors);
            }

            var blocking = errors.Where(e => e.Severity >= ValidationSeverity.Error).ToList();

            if (blocking.Count == 0)
            {
                valid.Add(record);
                metrics.TotalRecordsValidated++;
                continue;
            }

            metrics.TotalRecordsFailed++;
            TrackErrorCount(metrics, errors);

            deadLetters.Add(new DataFlowErrorRecord
            {
                PipelineName = _options.Name,
                Stage = DataFlowStage.Validate,
                ErrorType = nameof(CanonicalHalalRecordValidator),
                ErrorMessage = string.Join("; ", blocking.Select(e => $"{e.FieldName}: {e.Message}")),
                Context = errors.ToDictionary(e => e.FieldName, e => (object)e.Message)
            });
        }

        batch.Records = valid;
        await _deadLetterSink.WriteBatchAsync(deadLetters, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DataFlowBatchResult> LoadWithRetryAsync(
        DataFlowBatch<TRecord> batch,
        DataFlowMetrics metrics,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        Exception? last = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await _destination.LoadBatchAsync(batch, cancellationToken).ConfigureAwait(false);

                metrics.TotalRecordsLoaded += result.RecordsLoaded;
                metrics.TotalRecordsFailed += result.RecordsFailed;
                metrics.TotalBytesProcessed += result.BytesProcessed;
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                batch.RetryCount = attempt;

                _logger.LogWarning(ex,
                    "Load attempt {Attempt} of {Max} failed for pipeline {Pipeline}",
                    attempt, maxRetries, _options.Name);

                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        var failed = new DataFlowBatchResult
        {
            BatchId = batch.BatchId,
            PipelineName = _options.Name,
            Status = DataFlowBatchStatus.DeadLettered,
            RecordsFailed = batch.RecordCount,
            ErrorMessage = last?.Message,
            Exception = last
        };

        await _deadLetterSink.WriteAsync(new DataFlowErrorRecord
        {
            PipelineName = _options.Name,
            BatchId = batch.BatchId,
            Stage = DataFlowStage.Load,
            ErrorType = last?.GetType().Name ?? "Unknown",
            ErrorMessage = last?.Message ?? "Load failed after all retries.",
            StackTrace = last?.StackTrace,
            RetryCount = batch.RetryCount
        }, cancellationToken).ConfigureAwait(false);

        metrics.FailedBatches++;
        return failed;
    }

    private static void TrackErrorCount(DataFlowMetrics metrics, IEnumerable<ValidationError> errors)
    {
        foreach (var group in errors.GroupBy(e => e.ErrorCode))
        {
            metrics.ErrorCounts[group.Key] =
                metrics.ErrorCounts.GetValueOrDefault(group.Key) + group.Count();
        }
    }
}
