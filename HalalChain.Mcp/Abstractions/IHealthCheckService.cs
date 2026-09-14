using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Abstractions;

public interface IHealthCheckService
{
    Task<IReadOnlyList<HealthCheckResult>> CheckHealthAsync(CancellationToken ct = default);
}
