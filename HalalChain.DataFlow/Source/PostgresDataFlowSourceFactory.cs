using HalalChain.DataFlow.Configuration;
using Microsoft.Extensions.Logging;

namespace HalalChain.DataFlow.Source;

/// <summary>
/// Resolves a source's connection by name at construction time so a
/// misconfigured reference fails immediately and loudly rather than at the
/// first batch.
/// </summary>
public sealed class PostgresDataFlowSourceFactory : IDataFlowSourceFactory
{
    private readonly IReadOnlyDictionary<string, DataFlowConnectionOptions> _connections;
    private readonly ILoggerFactory _loggerFactory;

    public PostgresDataFlowSourceFactory(
        IReadOnlyDictionary<string, DataFlowConnectionOptions> connections,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(connections);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _connections = connections;
        _loggerFactory = loggerFactory;
    }

    public IDataFlowSource<TRecord> CreateSource<TRecord>(DataFlowSourceOptions options)
        where TRecord : class, new()
    {
        ArgumentNullException.ThrowIfNull(options);

        var connection = Resolve(options);
        return new PostgresDataFlowSource<TRecord>(
            options, connection, _loggerFactory.CreateLogger<PostgresDataFlowSource<TRecord>>());
    }

    public IDataFlowSource<Dictionary<string, object?>> CreateDynamicSource(DataFlowSourceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var connection = Resolve(options);
        return new DynamicPostgresDataFlowSource(
            options, connection, _loggerFactory.CreateLogger<DynamicPostgresDataFlowSource>());
    }

    private DataFlowConnectionOptions Resolve(DataFlowSourceOptions options)
    {
        if (!_connections.TryGetValue(options.ConnectionName, out var connection))
        {
            throw new InvalidOperationException(
                $"Data flow source '{options.Name}' references connection " +
                $"'{options.ConnectionName}', which is not configured. " +
                $"Known connections: {string.Join(", ", _connections.Keys)}.");
        }

        return connection;
    }
}
