using System.Data.Common;
using System.Text;
using HalalChain.DataFlow.Configuration;
using HalalChain.DataFlow.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HalalChain.DataFlow.Source;

/// <summary>
/// Reads batches of halal supply-chain records from PostgreSQL. Supports
/// full extraction, watermark-based incremental extraction, and keyset
/// pagination so large tables never load entirely into memory.
/// </summary>
public sealed class PostgresDataFlowSource<TRecord> : IDataFlowSource<TRecord>
    where TRecord : class, new()
{
    private readonly DataFlowSourceOptions _options;
    private readonly DataFlowConnectionOptions _connection;
    private readonly ILogger<PostgresDataFlowSource<TRecord>> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private NpgsqlDataSource? _dataSource;
    private DataFlowCheckpoint _checkpoint;
    private long _batchNumber;
    private bool _initialized;
    private bool _exhausted;
    private object? _lastWatermark;

    public PostgresDataFlowSource(
        DataFlowSourceOptions options,
        DataFlowConnectionOptions connection,
        ILogger<PostgresDataFlowSource<TRecord>> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);

        if (connection.Provider != DatabaseProvider.PostgreSQL)
        {
            throw new InvalidOperationException(
                $"Connection '{connection.Name}' is a {connection.Provider} connection. " +
                "PostgresDataFlowSource requires DatabaseProvider.PostgreSQL.");
        }

        _options = options;
        _connection = connection;
        _logger = logger;
        _checkpoint = new DataFlowCheckpoint
        {
            PipelineName = options.Name,
            SourceName = options.Name,
            WatermarkColumn = options.WatermarkColumn ?? string.Empty,
            WatermarkValue = options.WatermarkValue ?? new object()
        };
    }

    public string Name => _options.Name;
    public string EntityName => _options.EntityName;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            _dataSource = new NpgsqlDataSourceBuilder(_connection.ConnectionString).Build();
            _initialized = true;
            _logger.LogInformation(
                "PostgreSQL data flow source {Source} initialized for entity {Entity}",
                Name, EntityName);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DataFlowBatch<TRecord>> ExtractBatchAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var batch = new DataFlowBatch<TRecord>
        {
            PipelineName = Name,
            BatchNumber = Interlocked.Increment(ref _batchNumber),
            Status = DataFlowBatchStatus.Extracting
        };

        if (_exhausted)
        {
            batch.Status = DataFlowBatchStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;
            return batch;
        }

        try
        {
            // The data source is a pool, not a connection. Each batch opens
            // its own connection so a long-running extract never pins one.
            await using var connection = await _dataSource!.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);

            await using var command = CreateBatchCommand(connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                batch.Records.Add(MaterializeRecord(reader));
                CaptureWatermark(reader);
            }

            if (batch.RecordCount == 0)
            {
                _exhausted = true;
            }

            AdvanceCheckpoint(batch);

            batch.Status = DataFlowBatchStatus.Extracted;
            batch.CompletedAt = DateTimeOffset.UtcNow;

            _logger.LogDebug(
                "Extracted batch {BatchId} with {Count} records from {Source}",
                batch.BatchId, batch.RecordCount, Name);
        }
        catch (Exception ex)
        {
            batch.Status = DataFlowBatchStatus.Failed;
            batch.ErrorMessage = ex.Message;
            batch.Exception = ex;
            batch.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogError(ex, "Extraction failed for source {Source}", Name);
            throw;
        }

        return batch;
    }

    public Task<bool> HasMoreDataAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(!_exhausted);
    }

    public Task<DataFlowCheckpoint> GetCheckpointAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_checkpoint);
    }

    public Task SetCheckpointAsync(DataFlowCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(checkpoint);

        _checkpoint = checkpoint;
        _logger.LogDebug(
            "Checkpoint for {Source} advanced to batch {Batch} (watermark {Watermark})",
            Name, checkpoint.LastProcessedBatchNumber, checkpoint.WatermarkValue);
        return Task.CompletedTask;
    }

    public async Task<long> GetEstimatedRecordCountAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var table = ResolveRelationName();
        var sql = $"SELECT COUNT(*) FROM {table};";

        await using var connection = await _dataSource!.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout = _connection.CommandTimeoutSeconds;
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is long count ? count : 0L;
    }

    /// <summary>
    /// Builds the parameterized SELECT for the next batch. A configured
    /// query is used verbatim (so operators own all SQL); otherwise a
    /// keyset-paginated projection of the mapped table is generated.
    /// </summary>
    private NpgsqlCommand CreateBatchCommand(NpgsqlConnection connection)
    {
        var sql = _options.Query is { Length: > 0 } configured
            ? configured
            : BuildProjectionSql();

        var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout = _connection.CommandTimeoutSeconds;

        // Parameters injected by the generated projection. A caller-supplied
        // query may bind its own, so these are only added when the projection
        // was generated and the value is actually present.
        if (_options.Query is not { Length: > 0 } &&
            _options.WatermarkColumn is { Length: > 0 } &&
            _checkpoint.WatermarkValue is not null)
        {
            command.Parameters.AddWithValue("watermark", _checkpoint.WatermarkValue);
        }

        if (_options.Query is not { Length: > 0 })
        {
            command.Parameters.AddWithValue("batchSize", _options.BatchSize);
        }

        foreach (var (key, value) in _options.Parameters)
        {
            command.Parameters.AddWithValue(key,
                (object?)value ?? DBNull.Value);
        }

        return command;
    }

    private string BuildProjectionSql()
    {
        var table = ResolveRelationName();
        var columns = _options.Columns.Count > 0
            ? string.Join(", ", _options.Columns)
            : "*";

        var sql = new StringBuilder($"SELECT {columns} FROM {table}");

        if (_options.WatermarkColumn is { Length: > 0 } watermark && _checkpoint.WatermarkValue is not null)
        {
            sql.Append(" WHERE ").Append(QuoteIdentifier(watermark))
               .Append(" > @watermark");
        }

        if (_options.WatermarkColumn is { Length: > 0 } order)
        {
            sql.Append(" ORDER BY ").Append(QuoteIdentifier(order)).Append(" ASC");
        }

        sql.Append(" LIMIT @batchSize;");
        return sql.ToString();
    }

    private string ResolveRelationName()
    {
        if (!string.IsNullOrWhiteSpace(_options.Query))
        {
            throw new InvalidOperationException(
                $"Source '{Name}' uses a custom query; a table name must not also be set.");
        }

        if (string.IsNullOrWhiteSpace(_options.TableName))
        {
            throw new InvalidOperationException(
                $"Source '{Name}' must define either a Query or a TableName.");
        }

        var table = QuoteIdentifier(_options.TableName);
        return string.IsNullOrWhiteSpace(_options.SchemaName)
            ? table
            : $"{QuoteIdentifier(_options.SchemaName)}.{table}";
    }

    private TRecord MaterializeRecord(DbDataReader reader)
    {
        var record = new TRecord();
        var setters = BuildColumnSetters();

        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (!setters.TryGetValue(reader.GetName(i), out var setter)) continue;
            if (reader.IsDBNull(i))
            {
                setter(record, null);
                continue;
            }

            setter(record, reader.GetValue(i));
        }

        return record;
    }

    /// <summary>
    /// Reflects a writable-property map for the destination type. Unknown
    /// source columns are ignored rather than fatal, so a source table can
    /// gain columns without breaking the pipeline.
    /// </summary>
    private Dictionary<string, Action<TRecord, object?>> BuildColumnSetters()
    {
        var setters = new Dictionary<string, Action<TRecord, object?>>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in typeof(TRecord).GetProperties())
        {
            if (!property.CanWrite) continue;

            setters[property.Name] = (target, value) =>
            {
                if (value is null || !property.PropertyType.IsValueType)
                {
                    property.SetValue(target, value);
                    return;
                }

                var underlying = Nullable.GetUnderlyingType(property.PropertyType);
                if (underlying is not null)
                {
                    property.SetValue(target, Convert.ChangeType(value, underlying, System.Globalization.CultureInfo.InvariantCulture));
                    return;
                }

                property.SetValue(target, Convert.ChangeType(value, property.PropertyType, System.Globalization.CultureInfo.InvariantCulture));
            };
        }

        return setters;
    }

    private void AdvanceCheckpoint(DataFlowBatch<TRecord> batch)
    {
        _checkpoint.LastProcessedBatchNumber = batch.BatchNumber;
        _checkpoint.CheckpointAt = DateTimeOffset.UtcNow;

        if (_options.WatermarkColumn is { Length: > 0 } watermark && _lastWatermark is not null)
        {
            _checkpoint.WatermarkColumn = watermark;
            _checkpoint.WatermarkValue = _lastWatermark;
            _lastWatermark = null;
        }
    }

    /// <summary>
    /// Records the watermark value of the last row read so incremental runs
    /// resume from the correct position even when rows are out of order.
    /// </summary>
    private void CaptureWatermark(DbDataReader reader)
    {
        if (_options.WatermarkColumn is not { Length: > 0 } watermark) return;

        var ordinal = reader.GetOrdinal(watermark);
        if (ordinal < 0 || ordinal >= reader.FieldCount) return;

        _lastWatermark = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
    }

    /// <summary>Quotes a PostgreSQL identifier, rejecting embedded quotes.</summary>
    private static string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        if (identifier.Contains('"'))
        {
            throw new InvalidOperationException(
                $"Identifier '{identifier}' contains a double quote and cannot be safely quoted.");
        }

        return $"\"{identifier}\"";
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync().ConfigureAwait(false);
            _dataSource = null;
        }

        _gate.Dispose();
    }
}
