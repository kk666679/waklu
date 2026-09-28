using HalalChain.DataFlow.Configuration;
using Microsoft.Extensions.Logging;

namespace HalalChain.DataFlow.Destination;

/// <summary>
/// Resolves a destination's connection by name at construction time so a
/// misconfigured reference fails immediately rather than at first load.
/// </summary>
public sealed class PostgresDataFlowDestinationFactory : IDataFlowDestinationFactory
{
    private readonly IReadOnlyDictionary<string, DataFlowConnectionOptions> _connections;
    private readonly ILoggerFactory _loggerFactory;

    public PostgresDataFlowDestinationFactory(
        IReadOnlyDictionary<string, DataFlowConnectionOptions> connections,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(connections);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _connections = connections;
        _loggerFactory = loggerFactory;
    }

    public IDataFlowDestination<TRecord> CreateDestination<TRecord>(DataFlowDestinationOptions options)
        where TRecord : class, new()
    {
        ArgumentNullException.ThrowIfNull(options);

        var connection = Resolve(options);
        return new PostgresDataFlowDestination<TRecord>(
            options, connection, _loggerFactory.CreateLogger<PostgresDataFlowDestination<TRecord>>());
    }

    public IDataFlowDestination<Dictionary<string, object?>> CreateDynamicDestination(
        DataFlowDestinationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var connection = Resolve(options);
        return new DynamicPostgresDataFlowDestination(
            options, connection, _loggerFactory.CreateLogger<DynamicPostgresDataFlowDestination>());
    }

    private DataFlowConnectionOptions Resolve(DataFlowDestinationOptions options)
    {
        if (!_connections.TryGetValue(options.ConnectionName, out var connection))
        {
            throw new InvalidOperationException(
                $"Data flow destination '{options.Name}' references connection " +
                $"'{options.ConnectionName}', which is not configured. " +
                $"Known connections: {string.Join(", ", _connections.Keys)}.");
        }

        return connection;
    }
}
