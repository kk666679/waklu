using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HalalChain.Mcp.Services;

internal sealed class HealthCheckService : IHealthCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ServiceEndpoints _endpoints;
    private readonly ILogger<HealthCheckService> _logger;
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);

    public HealthCheckService(
        IHttpClientFactory httpClientFactory,
        IOptions<HalalChainOptions> options,
        ILogger<HealthCheckService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _endpoints = options.Value.Services;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HealthCheckResult>> CheckHealthAsync(CancellationToken ct = default)
    {
        var endpoints = new (string Name, string Url)[]
        {
            ("Platform API", $"{_endpoints.PlatformApi}/health/live"),
            ("HalalChain", $"{_endpoints.HalalChain}/health/live"),
            ("Marketplace", $"{_endpoints.Marketplace}/health/live"),
            ("Tawheed", $"{_endpoints.Tawheed}/health"),
            ("AI Inference", $"{_endpoints.AiInference}/health"),
        };

        var tasks = endpoints.Select(e => CheckEndpointAsync(e.Name, e.Url, ct));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    private async Task<HealthCheckResult> CheckEndpointAsync(string name, string url, CancellationToken ct)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("health");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(HealthCheckTimeout);

            var response = await client.GetAsync(url, cts.Token);

            return new HealthCheckResult
            {
                Service = name,
                Url = url,
                Status = response.IsSuccessStatusCode ? "healthy" : $"unhealthy ({response.StatusCode})",
                IsHealthy = response.IsSuccessStatusCode,
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Health check for {Service} timed out", name);
            return new HealthCheckResult
            {
                Service = name,
                Url = url,
                Status = "timeout",
                IsHealthy = false,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check for {Service} failed", name);
            return new HealthCheckResult
            {
                Service = name,
                Url = url,
                Status = $"unreachable ({ex.GetType().Name})",
                IsHealthy = false,
            };
        }
    }
}
