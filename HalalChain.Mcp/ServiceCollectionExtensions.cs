using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Services;
using HalalChain.Mcp.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace HalalChain.Mcp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHalalChainMcp(this IServiceCollection services, IConfiguration? configuration = null)
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

        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory, LoggerFactory>();

        services.AddSingleton<IPlatformDataService, PlatformDataService>();
        services.AddSingleton<IHealthCheckService, HealthCheckService>();
        services.AddSingleton<IToolRegistry, ToolRegistry>();

        services.AddSingleton<ProjectStatusTool>();
        services.AddSingleton<ListProjectsTool>();
        services.AddSingleton<ListPagesTool>();
        services.AddSingleton<ListComponentsTool>();
        services.AddSingleton<ListServicesTool>();
        services.AddSingleton<PlatformOverviewTool>();
        services.AddSingleton<GetArchitectureTool>();
        services.AddSingleton<HealthTool>();

        services.AddSingleton<ITool>(sp => sp.GetRequiredService<ProjectStatusTool>());
        services.AddSingleton<ITool>(sp => sp.GetRequiredService<ListProjectsTool>());
        services.AddSingleton<ITool>(sp => sp.GetRequiredService<ListPagesTool>());
        services.AddSingleton<ITool>(sp => sp.GetRequiredService<ListComponentsTool>());
        services.AddSingleton<ITool>(sp => sp.GetRequiredService<ListServicesTool>());
        services.AddSingleton<ITool>(sp => sp.GetRequiredService<PlatformOverviewTool>());
        services.AddSingleton<ITool>(sp => sp.GetRequiredService<GetArchitectureTool>());
        services.AddSingleton<ITool>(sp => sp.GetRequiredService<HealthTool>());

        return services;
    }
}
