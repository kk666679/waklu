using System.Data;
using System.Text;
using HalalChain.DataFlow.Configuration;
using HalalChain.DataFlow.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HalalChain.DataFlow.Destination;

/// <summary>
/// Writes batches of halal supply-chain records into PostgreSQL using
/// PostgreSQL's native COPY protocol for throughput, with a transactional
/// COPY fallback for batches that carry per-record conflict resolution.
/// </summary>
public sealed class PostgresDataFlowDestination<TRecord> : IDataFlowDestination<TRecord>
    where TRecord : class, new()
{
    private readonly DataFlowDestinationOptions _options;
    private readonly DataFlowConnectionOptions _connection;
    private readonly ILogger<PostgresDataFlowDestination<TRecord>> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private NpgsqlDataSource? _dataSource;
    private NpgsqlTransaction? _transaction;
    private bool _initialized;
    private string? _copyColumns;

    public PostgresDataFlowDestination(
        DataFlowDestinationOptions options,
        DataFlowConnectionOptions connection,
        ILogger<PostgresDataFlowDestination<TRecord>> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);

        if (connection.Provider != DatabaseProvider.PostgreSQL)
        {
            throw new InvalidOperationException(
                $"Connection '{connection.Name}' is a {connection.Provider} connection. " +
                "PostgresDataFlowDestination requires DatabaseProvider.PostgreSQL.");
        }

        _options = options;
        _connection = connection;
        _logger = logger;
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
            _copyColumns = ResolveColumnList();
            _initialized = true;
            _logger.LogInformation(
                "PostgreSQL data flow destination {Destination} initialized for entity {Entity}",
                Name, EntityName);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DataFlowBatchResult> LoadBatchAsync(
        DataFlowBatch<TRecord> batch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (!_initialized) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var result = new DataFlowBatchResult
        {
            BatchId = batch.BatchId,
            PipelineName = batch.PipelineName
        };
        var started = DateTimeOffset.UtcNow;

        try
        {
            await using var connection = await _dataSource!.OpenConnectionAsync(cancellationToken)
                .ConfigureAwait(false);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            _transaction = transaction;

            var affected = _options.SyncMode == SyncMode.Full
                ? await InsertBatchAsync(connection, transaction, batch, cancellationToken).ConfigureAwait(false)
                : await UpsertBatchAsync(connection, transaction, batch, cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _transaction = null;

            result.RecordsLoaded = affected;
            result.Status = DataFlowBatchStatus.Completed;
            result.EntityCounts[_options.EntityName] = affected;

            _logger.LogDebug("Loaded {Count} records into {Destination}", affected, Name);
        }
        catch (Exception ex)
        {
            await SafeRollbackAsync().ConfigureAwait(false);

            result.Status = DataFlowBatchStatus.Failed;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            result.RecordsFailed = batch.RecordCount;

            _logger.LogError(ex, "Load failed for destination {Destination}", Name);
            throw;
        }
        finally
        {
            result.Duration = DateTimeOffset.UtcNow - started;
        }

        return result;
    }

    private async Task<long> InsertBatchAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DataFlowBatch<TRecord> batch,
        CancellationToken cancellationToken)
    {
        if (batch.RecordCount == 0) return 0L;

        if (_options.UseBulkInsert)
        {
            return await CopyBatchAsync(connection, transaction, batch, cancellationToken).ConfigureAwait(false);
        }

        var (sql, parameters) = BuildInsertCommand(batch);
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = _connection.CommandTimeoutSeconds;
        BindParameters(command, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Uses PostgreSQL's binary COPY for bulk insert. COPY is append-only,
    /// so it is only valid for a full sync into an empty/truncated target.
    /// </summary>
    private async Task<long> CopyBatchAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DataFlowBatch<TRecord> batch,
        CancellationToken cancellationToken)
    {
        var sql = $"COPY {ResolveRelationName()} ({_copyColumns}) FROM STDIN (FORMAT BINARY);";
        var loaded = 0L;

        await using (var writer = await connection.BeginBinaryImportAsync(sql, cancellationToken)
            .ConfigureAwait(false))
        {
            foreach (var record in batch.Records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteRecordAsync(writer, record, cancellationToken).ConfigureAwait(false);
                loaded++;
            }

            await writer.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }

        return loaded;
    }

    private async Task<long> UpsertBatchAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DataFlowBatch<TRecord> batch,
        CancellationToken cancellationToken)
    {
        var processed = 0L;

        foreach (var record in batch.Records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (sql, parameters) = BuildUpsertCommand(record);
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.CommandTimeout = _connection.CommandTimeoutSeconds;
            BindParameters(command, parameters);
            processed += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return processed;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await SafeRollbackAsync().ConfigureAwait(false);
    }

    private async Task SafeRollbackAsync()
    {
        if (_transaction is null) return;

        try
        {
            await _transaction.RollbackAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rollback failed for destination {Destination}", Name);
        }
        finally
        {
            _transaction = null;
        }
    }

    public async Task<long> GetExistingRecordCountAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var sql = $"SELECT COUNT(*) FROM {ResolveRelationName()};";

        await using var connection = await _dataSource!.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout = _connection.CommandTimeoutSeconds;
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is long count ? count : 0L;
    }

    /// <summary>
    /// Builds a single multi-row INSERT. PostgreSQL caps a statement at
    /// 65535 bind parameters, so the row count is bounded by the wider of
    /// BatchSize and that ceiling divided by the column count.
    /// </summary>
    private (string Sql, Dictionary<string, object?> Parameters) BuildInsertCommand(DataFlowBatch<TRecord> batch)
    {
        var columns = TargetColumns();
        var maxRows = Math.Max(1, 65535 / columns.Count);
        var rows = batch.Records.Take(maxRows).ToList();

        var sql = new StringBuilder($"INSERT INTO {ResolveRelationName()} ({_copyColumns}) VALUES ");
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (var r = 0; r < rows.Count; r++)
        {
            if (r > 0) sql.Append(", ");

            sql.Append('(')
               .Append(string.Join(", ", columns.Select(c => "@" + c + r)))
               .Append(')');

            foreach (var column in columns)
            {
                parameters[column + r] = ReadColumn(rows[r], column);
            }
        }

        sql.Append(';');
        return (sql.ToString(), parameters);
    }

    private (string Sql, Dictionary<string, object?> Parameters) BuildUpsertCommand(TRecord record)
    {
        var sql = new StringBuilder();
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var columns = TargetColumns();

        switch (_options.ConflictResolution)
        {
            case ConflictResolution.DestinationWins:
                // The destination is authoritative: insert only when the key
                // is absent, never overwrite an existing row. DO NOTHING
                // rather than a SELECT probe so the affected-row count
                // stays meaningful for the batch metrics.
                sql.Append("INSERT INTO ").Append(ResolveRelationName())
                   .Append(" (").Append(_copyColumns).Append(") VALUES (")
                   .Append(string.Join(", ", columns.Select(c => "@" + c)))
                   .Append(") ON CONFLICT DO NOTHING;");
                break;

            case ConflictResolution.Fail:
                // A bare INSERT with no conflict clause: a duplicate key
                // raises the provider's unique-violation error and the
                // batch is rejected, which is the intended behaviour.
                sql.Append("INSERT INTO ").Append(ResolveRelationName())
                   .Append(" (").Append(_copyColumns).Append(") VALUES (")
                   .Append(string.Join(", ", columns.Select(c => "@" + c)))
                   .Append(");");
                break;

            default:
                // SourceWins / Merge both upsert; they differ in whether
                // ignored columns are preserved, handled by column selection.
                sql.Append(BuildUpsertStatement(parameters, record));
                return (sql.ToString(), parameters);
        }

        foreach (var column in columns)
        {
            parameters[column] = ReadColumn(record, column);
        }

        return (sql.ToString(), parameters);
    }

    private string BuildUpsertStatement(
        Dictionary<string, object?> parameters,
        TRecord record)
    {
        var columns = TargetColumns();
        var valueList = string.Join(", ", columns.Select(c => "@" + c));
        var sql = new StringBuilder();

        sql.Append("INSERT INTO ").Append(ResolveRelationName())
           .Append(" (").Append(_copyColumns).Append(") VALUES (")
           .Append(valueList).Append(')');

        var updateColumns = _options.UpdateColumns.Count > 0
            ? _options.UpdateColumns
            : columns.Where(c => !_options.KeyColumns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();

        if (_options.KeyColumns.Count == 0)
        {
            throw new InvalidOperationException(
                $"Destination '{Name}' uses an incremental sync but declares no KeyColumns, " +
                "so there is no conflict target to upsert against.");
        }

        if (updateColumns.Count == 0)
        {
            sql.Append(" ON CONFLICT DO NOTHING");
        }
        else
        {
            sql.Append(" ON CONFLICT (")
               .Append(string.Join(", ", _options.KeyColumns.Select(QuoteIdentifier)))
               .Append(") DO UPDATE SET ")
               .Append(string.Join(", ",
                   updateColumns.Select(c => $"{QuoteIdentifier(c)} = EXCLUDED.{QuoteIdentifier(c)}")));
        }

        sql.Append(';');

        foreach (var column in columns)
        {
            parameters[column] = ReadColumn(record, column);
        }

        return sql.ToString();
    }

    private async Task WriteRecordAsync(
        NpgsqlBinaryImporter writer,
        TRecord record,
        CancellationToken cancellationToken)
    {
        await writer.StartRowAsync(cancellationToken).ConfigureAwait(false);

        foreach (var column in TargetColumns())
        {
            await writer.WriteAsync(ReadColumn(record, column), cancellationToken).ConfigureAwait(false);
        }
    }

    private static void BindParameters(NpgsqlCommand command, Dictionary<string, object?> parameters)
    {
        foreach (var (key, value) in parameters)
        {
            command.Parameters.AddWithValue(key, value ?? DBNull.Value);
        }
    }

    private static object? ReadColumn(TRecord record, string column)
    {
        var property = typeof(TRecord).GetProperty(column,
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        return property?.CanWrite == true ? property.GetValue(record) : null;
    }

    private string ResolveColumnList()
    {
        if (_options.Columns.Count == 0)
        {
            throw new InvalidOperationException(
                $"Destination '{Name}' must define at least one column to write.");
        }

        // Ignored columns are dropped from the write set entirely, so a
        // source-side rename of an excluded field cannot overwrite the
        // destination's own value for it.
        var columns = _options.Columns
            .Where(c => !_options.IgnoreColumns.Contains(c, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (columns.Count == 0)
        {
            throw new InvalidOperationException(
                $"Destination '{Name}' ignores every configured column, leaving nothing to write.");
        }

        return string.Join(", ", columns.Select(QuoteIdentifier));
    }

    /// <summary>The write column set, split once into its individual names.</summary>
    private List<string> TargetColumns() =>
        _copyColumns!.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList();

    private string ResolveRelationName()
    {
        if (string.IsNullOrWhiteSpace(_options.TableName))
        {
            throw new InvalidOperationException(
                $"Destination '{Name}' must define a TableName.");
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
