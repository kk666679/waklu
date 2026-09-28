using System.Data.Common;
using System.Text;
using HalalChain.DataFlow.Configuration;
using HalalChain.DataFlow.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HalalChain.DataFlow.Source;

/// <summary>
/// Schema-agnostic source. Returns each row as a column-name to value
/// dictionary so a pipeline can be defined in configuration alone, before
/// anyone has written a C# record type for the upstream system. This is the
/// path most enterprise integrations take, because the source schema is
/// owned by another team's system.
/// </summary>
public sealed class DynamicPostgresDataFlowSource : IDataFlowSource<Dictionary<string, object?>>
{
    private readonly DataFlowSourceOptions _options;
    private readonly DataFlowConnectionOptions _connection;
    private readonly ILogger<DynamicPostgresDataFlowSource> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private NpgsqlDataSource? _dataSource;
    private DataFlowCheckpoint _checkpoint;
    private long _batchNumber;
    private bool _initialized;
    private bool _exhausted;

    public DynamicPostgresDataFlowSource(
        DataFlowSourceOptions options,
        DataFlowConnectionOptions connection,
        ILogger<DynamicPostgresDataFlowSource> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);

        if (connection.Provider != DatabaseProvider.PostgreSQL)
        {
            throw new InvalidOperationException(
                $"Connection '{connection.Name}' is a {connection.Provider} connection. " +
                "DynamicPostgresDataFlowSource requires DatabaseProvider.PostgreSQL.");
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
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DataFlowBatch<Dictionary<string, object?>>> ExtractBatchAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_initialized) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var batch = new DataFlowBatch<Dictionary<string, object?>>
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
            await using var connection = await _dataSource!.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = new NpgsqlCommand(BuildSql(), connection);
            command.CommandTimeout = _connection.CommandTimeoutSeconds;
            BindParameters(command);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var columnNames = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                columnNames[i] = reader.GetName(i);
            }

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[columnNames[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }

                batch.Records.Add(row);
                CaptureWatermark(reader, row);
            }

            if (batch.RecordCount == 0) _exhausted = true;

            _checkpoint.LastProcessedBatchNumber = batch.BatchNumber;
            _checkpoint.CheckpointAt = DateTimeOffset.UtcNow;

            batch.Status = DataFlowBatchStatus.Extracted;
            batch.CompletedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            batch.Status = DataFlowBatchStatus.Failed;
            batch.ErrorMessage = ex.Message;
            batch.Exception = ex;
            batch.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogError(ex, "Dynamic extraction failed for source {Source}", Name);
            throw;
        }

        return batch;
    }

    public Task<bool> HasMoreDataAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(!_exhausted);
    }

    public Task<DataFlowCheckpoint> GetCheckpointAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_checkpoint);

    public Task SetCheckpointAsync(DataFlowCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        _checkpoint = checkpoint;
        return Task.CompletedTask;
    }

    public async Task<long> GetEstimatedRecordCountAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = await _dataSource!.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {ResolveRelationName()};", connection);
        command.CommandTimeout = _connection.CommandTimeoutSeconds;
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is long count ? count : 0L;
    }

    private string BuildSql()
    {
        if (_options.Query is { Length: > 0 } configured) return configured;

        var columns = _options.Columns.Count > 0 ? string.Join(", ", _options.Columns) : "*";
        var sql = new StringBuilder($"SELECT {columns} FROM {ResolveRelationName()}");

        if (_options.WatermarkColumn is { Length: > 0 } watermark)
        {
            sql.Append(" WHERE ").Append(QuoteIdentifier(watermark)).Append(" > @watermark");
            sql.Append(" ORDER BY ").Append(QuoteIdentifier(watermark)).Append(" ASC");
        }

        sql.Append(" LIMIT @batchSize;");
        return sql.ToString();
    }

    private void BindParameters(NpgsqlCommand command)
    {
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
            command.Parameters.AddWithValue(key, (object?)value ?? DBNull.Value);
        }
    }

    private void CaptureWatermark(DbDataReader reader, Dictionary<string, object?> row)
    {
        if (_options.WatermarkColumn is not { Length: > 0 } watermark) return;

        if (row.TryGetValue(watermark, out var value) && value is not null)
        {
            _checkpoint.WatermarkValue = value;
        }
    }

    private string ResolveRelationName()
    {
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
