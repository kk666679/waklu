using HalalChain.Agents.Eval;
using HalalChain.Agents.Eval.Dag.Scorers;
using HalalChain.Agents.Eval.Traces;
using HalalChain.Agents.Runtime;
using HalalChain.Agents.Traces;
using HalalChain.Application.Agentic.Abstractions;
using HalalChain.Application.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HalalChain.Agents.DependencyInjection;

public static class AgentsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the eval harness. Read-only: it scores traces against goldens
    /// and never writes to evidence storage.
    /// </summary>
    public static IServiceCollection AddAgentEvaluation(
        this IServiceCollection services, NodeThresholds? thresholds = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(thresholds ?? new NodeThresholds());
        services.AddSingleton<NodeScorerRegistry>();
        services.AddSingleton<AgentTraceEvaluator>();
        services.AddScoped<ITraceLoader, BlobStoreTraceLoader>();
        services.AddScoped<IAgentTraceStore, InMemoryAgentTraceIndex>();
        services.AddScoped<ITraceResolver, IndexedTraceResolver>();

        return services;
    }

    /// <summary>
    /// Registers the agent runtime client and the budget ledger.
    ///
    /// The typed <see cref="HttpClient"/> has no BaseAddress fallback: if
    /// Agents:BaseUrl is missing the client fails to configure and every call
    /// throws at send time. A default URL that quietly points at localhost in
    /// production is the kind of default that turns a configuration mistake
    /// into a silent no-op.
    /// </summary>
    public static IServiceCollection AddAgentRuntime(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AgentsRuntimeOptions>(configuration.GetSection(AgentsRuntimeOptions.SectionName));

        services.AddHttpClient<IAgentWorkflow, AgentWorkflowClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<AgentsRuntimeOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                throw new InvalidOperationException(
                    "Agents:BaseUrl is not configured. The agent runtime has no " +
                    "in-process fallback by design — an unreachable agents service " +
                    "must fail, not degrade.");
            }

            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            client.Timeout = options.Timeout;
        });

        services.AddSingleton<IWorkflowBudget, WorkflowBudgetLedger>();

        return services;
    }
}
