using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

public sealed class HealthTool : ITool
{
    private readonly IHealthCheckService _healthCheck;

    public HealthTool(IHealthCheckService healthCheck)
    {
        _healthCheck = healthCheck;
    }

    public string Name => "halalchain_health";

    public string Description => "Checks the health of HalalChain platform services by hitting their health endpoints.";

    public object InputSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.NetworkReadOnly("HalalChain service health");

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var results = await _healthCheck.CheckHealthAsync(ct);
        var healthy = results.Count(r => r.IsHealthy);
        var total = results.Count;

        var output = $"# Health Check Results\n\n";
        output += $"**{healthy}/{total} services healthy**\n\n";

        output += "| Service | Status | URL |\n|---------|--------|-----|\n";
        foreach (var r in results)
        {
            var icon = r.IsHealthy ? "✅" : "❌";
            output += $"| {icon} {r.Service} | {r.Status} | `{r.Url}` |\n";
        }

        if (healthy < total)
        {
            output += $"\n⚠️ {total - healthy} service(s) are not reachable. ";
            output += "Ensure Docker containers are running with `docker compose up`.";
        }

        return output;
    }
}
