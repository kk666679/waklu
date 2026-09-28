using HalalChain.DataFlow.Configuration;
using HalalChain.DataFlow.Destination;
using HalalChain.DataFlow.Models;
using HalalChain.DataFlow.Source;
using HalalChain.DataFlow.Transform;
using HalalChain.DataFlow.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HalalChain.DataFlow.DependencyInjection;

public static class DataFlowServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PostgreSQL data flow components: source, destination,
    /// canonical-record validation, and the dead-letter sink. Connection
    /// strings come from configuration, never from hardcoded values.
    /// </summary>
    public static IServiceCollection AddHalalChainDataFlow(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DataFlowOptions>(configuration.GetSection(DataFlowOptions.SectionName));

        // The validators and the key resolver are stateless with respect to a
        // single run, so singletons are safe and avoid per-batch allocation.
        services.TryAddSingleton<IEntityKeyResolver, InMemoryEntityKeyResolver>();
        services.TryAddSingleton<IDataFlowValidator<CanonicalHalalRecord>, CanonicalHalalRecordValidator>();

        services.TryAddSingleton<IDeadLetterSink>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DataFlowOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<FileSystemDeadLetterSink>>();
            var directory = options.Global.DeadLetterDirectory;

            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.Combine(Path.GetTempPath(), "halalchain", "deadletter");
            }

            return new FileSystemDeadLetterSink(directory, logger);
        });

        services.TryAddSingleton(sp => CreateSourceFactory(sp));
        services.TryAddSingleton(sp => CreateDestinationFactory(sp));

        return services;
    }

    private static IDataFlowSourceFactory CreateSourceFactory(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<DataFlowOptions>>().Value;
        var connections = BuildConnectionMap(options);
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        return new PostgresDataFlowSourceFactory(connections, loggerFactory);
    }

    private static IDataFlowDestinationFactory CreateDestinationFactory(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<DataFlowOptions>>().Value;
        var connections = BuildConnectionMap(options);
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        return new PostgresDataFlowDestinationFactory(connections, loggerFactory);
    }

    private static Dictionary<string, DataFlowConnectionOptions> BuildConnectionMap(DataFlowOptions options) =>
        options.Connections.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
}
