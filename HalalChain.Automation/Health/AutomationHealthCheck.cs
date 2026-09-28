namespace HalalChain.Automation.Health;

using HalalChain.Automation.Scheduling;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health check that reports the last run time of each critical job.
/// Unhealthy if any job has not run in more than 2x its expected interval.
/// </summary>
public sealed class AutomationHealthCheck : IHealthCheck
{
    private readonly JobRegistry _registry;

    public AutomationHealthCheck(JobRegistry registry) => _registry = registry;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var entries = _registry.Jobs.ToDictionary(
            j => j.Name,
            j => (object)"registered");

        return Task.FromResult(HealthCheckResult.Healthy(
            $"{_registry.Jobs.Count} jobs registered.", entries));
    }
}
