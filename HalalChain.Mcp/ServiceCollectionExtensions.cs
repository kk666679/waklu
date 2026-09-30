using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Services;
using HalalChain.Mcp.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HalalChain.Mcp;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP server. Pass <paramref name="transportWriter"/> when
    /// hosting the stdio loop in-process; without it the messenger writes to
    /// <see cref="TextWriter.Null"/>, which keeps the container usable from
    /// tests that only exercise the registries.
    /// </summary>
    public static IServiceCollection AddHalalChainMcp(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        TransportWriter? transportWriter = null)
    {
        var configBuilder = new ConfigurationBuilder();

        if (configuration != null)
        {
            configBuilder.AddConfiguration(configuration);
        }

        configBuilder
            .AddEnvironmentVariables(HalalChainOptions.GetEnvironmentVariablePrefix().TrimEnd('_'));

        var config = configBuilder.Build();

        services.Configure<HalalChainOptions>(options =>
        {
            config.GetSection(HalalChainOptions.SectionName).Bind(options);
        });

        services.AddHttpClient("health", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton<TransportWriter>(transportWriter ?? new TransportWriter(TextWriter.Null));

        // stdout belongs to the JSON-RPC transport, so diagnostics go to stderr.
        // A bare LoggerFactory with no providers would silently drop everything,
        // hence the explicit stderr provider.
        services.AddSingleton(sp => new StderrLoggerProvider(MapLogLevel(
            sp.GetRequiredService<IOptions<HalalChainOptions>>().Value.Mcp.LogLevel)));

        services.AddSingleton<ILoggerFactory>(sp => LoggerFactory.Create(
            builder => builder.AddProvider(sp.GetRequiredService<StderrLoggerProvider>())));

        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        services.AddSingleton<IMcpSession, McpSession>();
        services.AddSingleton<IClientMessenger, ClientMessenger>();

        services.AddSingleton<IPlatformDataService, PlatformDataService>();
        services.AddSingleton<IHealthCheckService, HealthCheckService>();
        services.AddSingleton<ICodeIntrospectionService, CodeIntrospectionService>();
        services.AddSingleton<IGovernanceService, GovernanceService>();
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddSingleton<IResourceRegistry, ResourceRegistry>();
        services.AddSingleton<IPromptRegistry, PromptRegistry>();

        // The original eight tools.
        RegisterTool<ProjectStatusTool>(services);
        RegisterTool<ListProjectsTool>(services);
        RegisterTool<ListPagesTool>(services);
        RegisterTool<ListComponentsTool>(services);
        RegisterTool<ListServicesTool>(services);
        RegisterTool<PlatformOverviewTool>(services);
        RegisterTool<GetArchitectureTool>(services);
        RegisterTool<HealthTool>(services);

        // Wave 1: code introspection (8 read-only tools).
        RegisterTool<ListEndpointsTool>(services);
        RegisterTool<ListMediatrHandlersTool>(services);
        RegisterTool<ListDomainEventsTool>(services);
        RegisterTool<ListAggregateRootsTool>(services);
        RegisterTool<ListValueObjectsTool>(services);
        RegisterTool<ListMigrationsTool>(services);
        RegisterTool<TestInventoryTool>(services);
        RegisterTool<FindTypeTool>(services);

        // Wave 2: governance introspection (9 read-only tools).
        RegisterTool<GetPrinciplesTool>(services);
        RegisterTool<ListAdrsTool>(services);
        RegisterTool<GetAdrTool>(services);
        RegisterTool<ListSkillsTool>(services);
        RegisterTool<GetSkillTool>(services);
        RegisterTool<GetControlsTool>(services);
        RegisterTool<GetServiceManifestTool>(services);
        RegisterTool<VerifyNoVerdictAuthorityTool>(services);
        RegisterTool<ListGovernanceWorkflowsTool>(services);

        return services;
    }

    /// <summary>
    /// Registers a tool twice: once concrete, so a host can resolve the type,
    /// and once as <see cref="ITool"/>, which is how
    /// <see cref="ToolRegistry"/> discovers it.
    /// </summary>
    private static void RegisterTool<TTool>(IServiceCollection services)
        where TTool : class, ITool
    {
        services.AddSingleton<TTool>();
        services.AddSingleton<ITool>(sp => sp.GetRequiredService<TTool>());
    }

    private static LogLevel MapLogLevel(LogLevelOption level) => level switch
    {
        LogLevelOption.Trace => LogLevel.Trace,
        LogLevelOption.Debug => LogLevel.Debug,
        LogLevelOption.Warning => LogLevel.Warning,
        LogLevelOption.Error => LogLevel.Error,
        LogLevelOption.Critical => LogLevel.Critical,
        LogLevelOption.None => LogLevel.None,
        _ => LogLevel.Information,
    };
}
