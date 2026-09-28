using System.Text;
using HalalChain.DataFlow.Configuration;
using HalalChain.DataFlow.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HalalChain.DataFlow.Destination;

/// <summary>
/// Schema-agnostic destination. Writes column-name to value dictionaries,
/// so an integration can be defined entirely in configuration. Identifiers
/// are validated rather than interpolated blindly, because in this mode the
/// column names come from configuration rather than a compiled type.
/// </summary>
public sealed class DynamicPostgresDataFlowDestination : IDataFlowDestination<Dictionary<string, object?>>
{
    private readonly DataFlowDestinationOptions _options;
    private readonly DataFlowConnectionOptions _connection;
    private readonly ILogger<DynamicPostgresDataFlowDestination> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private NpgsqlDataSource? _dataSource;
    private NpgsqlTransaction? _transaction;
    private bool _initialized;
    private List<string> _columns = new();

    public DynamicPostgresDataFlowDestination(
        DataFlowDestinationOptions options,
        DataFlowConnectionOptions connection,
        ILogger<DynamicPostgresDataFlowDestination> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);

        if (connection.Provider != DatabaseProvider.PostgreSQL)
        {
            throw new InvalidOperationException(
                $"Connection '{connection.Name}' is a {connection.Provider} connection. " +
                "DynamicPostgresDataFlowDestination requires DatabaseProvider.PostgreSQL.");
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
            _columns = ResolveColumns();
            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DataFlowBatchResult> LoadBatchAsync(
        DataFlowBatch<Dictionary<string, object?>> batch,
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

            var processed = 0L;

            foreach (var record in batch.Records)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (sql, parameters) = _options.SyncMode == SyncMode.Full
                    ? BuildInsert(record)
                    : BuildUpsert(record);

                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.CommandTimeout = _connection.CommandTimeoutSeconds;

                foreach (var (key, value) in parameters)
                {
                    command.Parameters.AddWithValue(key, value ?? DBNull.Value);
                }

                processed += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _transaction = null;

            result.RecordsLoaded = processed;
            result.Status = DataFlowBatchStatus.Completed;
            result.EntityCounts[_options.EntityName] = processed;
        }
        catch (Exception ex)
        {
            await SafeRollbackAsync().ConfigureAwait(false);

            result.Status = DataFlowBatchStatus.Failed;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            result.RecordsFailed = batch.RecordCount;

            _logger.LogError(ex, "Dynamic load failed for destination {Destination}", Name);
            throw;
        }
        finally
        {
            result.Duration = DateTimeOffset.UtcNow - started;
        }

        return result;
    }

    private (string Sql, Dictionary<string, object?> Parameters) BuildInsert(
        Dictionary<string, object?> record)
    {
        var sql = new StringBuilder($"INSERT INTO {ResolveRelationName()} ({ColumnList()}) VALUES (")
            .Append(string.Join(", ", _columns.Select(c => "@" + c)))
            .Append(')')
            .Append(';');

        return (sql.ToString(), BindRecord(record));
    }

    private (string Sql, Dictionary<string, object?> Parameters) BuildUpsert(
        Dictionary<string, object?> record)
    {
        var parameters = BindRecord(record);
        var sql = new StringBuilder($"INSERT INTO {ResolveRelationName()} ({ColumnList()}) VALUES (")
            .Append(string.Join(", ", _columns.Select(c => "@" + c)))
            .Append(')');

        if (_options.ConflictResolution == ConflictResolution.Fail)
        {
            sql.Append(';');
            return (sql.ToString(), parameters);
        }

        if (_options.ConflictResolution == ConflictResolution.DestinationWins)
        {
            sql.Append(" ON CONFLICT DO NOTHING;");
            return (sql.ToString(), parameters);
        }

        var updateColumns = _options.UpdateColumns.Count > 0
            ? _options.UpdateColumns
            : _columns.Where(c => !_options.KeyColumns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();

        if (_options.KeyColumns.Count == 0)
        {
            throw new InvalidOperationException(
                $"Destination '{Name}' uses an incremental sync but declares no KeyColumns, " +
                "so there is no conflict target to upsert against.");
        }

        if (updateColumns.Count == 0)
        {
            sql.Append(" ON CONFLICT DO NOTHING;");
            return (sql.ToString(), parameters);
        }

        sql.Append(" ON CONFLICT (")
           .Append(string.Join(", ", _options.KeyColumns.Select(QuoteIdentifier)))
           .Append(") DO UPDATE SET ")
           .Append(string.Join(", ", updateColumns.Select(c => $"{QuoteIdentifier(c)} = EXCLUDED.{QuoteIdentifier(c)}")))
           .Append(';');

        return (sql.ToString(), parameters);
    }

    private Dictionary<string, object?> BindRecord(Dictionary<string, object?> record)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in _columns)
        {
            parameters[column] = record.TryGetValue(column, out var value) ? value : null;
        }

        return parameters;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default) =>
        await SafeRollbackAsync().ConfigureAwait(false);

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

        await using var connection = await _dataSource!.OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {ResolveRelationName()};", connection);
        command.CommandTimeout = _connection.CommandTimeoutSeconds;
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is long count ? count : 0L;
    }

    private List<string> ResolveColumns()
    {
        if (_options.Columns.Count == 0)
        {
            throw new InvalidOperationException(
                $"Destination '{Name}' must define at least one column to write.");
        }

        var raw = _options.Columns
            .Where(c => !_options.IgnoreColumns.Contains(c, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (raw.Count == 0)
        {
            throw new InvalidOperationException(
                $"Destination '{Name}' ignores every configured column, leaving nothing to write.");
        }

        // Bind parameter names cannot be quoted, so the raw column name is
        // what keys the parameter dictionary; the SQL side quotes separately.
        return raw;
    }

    private string ColumnList() =>
        string.Join(", ", _columns.Select(QuoteIdentifier));

    private string ResolveRelationName()
    {
        if (string.IsNullOrWhiteSpace(_options.TableName))
        {
            throw new InvalidOperationException($"Destination '{Name}' must define a TableName.");
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
