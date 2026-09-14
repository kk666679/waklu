using HalalChain.Components.Shared;

namespace HalalChain.Services;

/// <summary>
/// Provides platform overview data — service registry, verification flow,
/// architecture diagram, and health endpoint information.
/// </summary>
public class PlatformOverviewService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PlatformOverviewService> _logger;

    public PlatformOverviewService(
        IHttpClientFactory httpClientFactory,
        ILogger<PlatformOverviewService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public PlatformOverviewData GetOverview()
    {
        return new PlatformOverviewData
        {
            Services = GetServices(),
            VerificationFlowSteps = GetVerificationFlowSteps(),
            ArchitectureDiagram = GetArchitectureDiagram(),
            HealthEndpoints = GetHealthEndpoints(),
        };
    }

    public async Task CheckServiceHealthAsync(ServiceInfo service)
    {
        if (string.IsNullOrEmpty(service.HealthUrl))
        {
            service.Status = ServiceStatus.Unknown;
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("PlatformApi");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var response = await client.GetAsync(service.HealthUrl, cts.Token);
            service.Status = response.IsSuccessStatusCode
                ? ServiceStatus.Running
                : ServiceStatus.Stopped;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check failed for {ServiceName} at {Url}", service.Name, service.HealthUrl);
            service.Status = ServiceStatus.Stopped;
        }
    }

    public async Task CheckAllHealthAsync(PlatformOverviewData overview)
    {
        var tasks = overview.Services
            .Where(s => !string.IsNullOrEmpty(s.HealthUrl))
            .Select(CheckServiceHealthAsync);

        await Task.WhenAll(tasks);
    }

    private static List<ServiceInfo> GetServices() =>
    [
        new() { Name = "Platform API",  Port = 5001, Type = ".NET 10 REST + Swagger", Tags = ["dotnet"], HealthUrl = "http://localhost:5001/health/live" },
        new() { Name = "HalalChain",    Port = 5200, Type = ".NET 10 Blazor Server",  Tags = ["dotnet"], HealthUrl = "http://localhost:5200/health/live" },
        new() { Name = "Marketplace",   Port = 5201, Type = ".NET 10 MVC",            Tags = ["dotnet"], HealthUrl = "http://localhost:5201/health/live" },
        new() { Name = "AI Inference",  Port = 7071, Type = "Python / Node",          Tags = ["python", "node"] },
        new() { Name = "Tawheed",       Port = 8000, Type = "Python FastAPI",         Tags = ["python"], HealthUrl = "http://localhost:8000/health" },
        new() { Name = "PostgreSQL",    Port = 5432, Type = "Infra v17",              Tags = ["infra"] },
        new() { Name = "Redis",         Port = 6379, Type = "Infra v7",               Tags = ["infra"] },
        new() { Name = "Qdrant",        Port = 6333, Type = "Infra Vector DB",        Tags = ["infra"] },
        new() { Name = "Neo4j",         Port = 7687, Type = "Infra Graph (planned)",  Tags = ["infra"] },
    ];

    private static List<string> GetVerificationFlowSteps() =>
    [
        "Certificate Evidence",
        "AI Agents (Tawheed)",
        "Evidence Store",
        "Policy Engine",
        "✅ Verified / ⚠️ Review / ❌ Rejected",
    ];

    private static string GetArchitectureDiagram() =>
        """
        Browser
          ↓
        HalalChain (Blazor) / Marketplace (MVC)  ← JWT auth
          ↓
        Platform API (.NET 10) — Modular Monolith
          ├── Modules/Identity, Catalog, Halal, Commerce, AI
          ├── PostgreSQL ← Catalog, Vendors, Orders, Halal records
          ├── Redis ← Cart session, cache
          ├── ai-inference ← Embeddings, classify, rerank
          └── tawheed ← Multi-agent evidence + Policy Engine
        """;

    private static List<HealthEndpointInfo> GetHealthEndpoints() =>
    [
        new()
        {
            Endpoint = "/health",
            Description = "Diagnostic aggregate",
            Services = ["Platform API", "HalalChain", "Marketplace", "Tawheed"],
        },
        new()
        {
            Endpoint = "/health/live",
            Description = "Liveness probe (no external deps)",
            Services = ["Platform API", "HalalChain", "Marketplace"],
        },
        new()
        {
            Endpoint = "/health/ready",
            Description = "Readiness probe (critical deps)",
            Services = ["Platform API", "HalalChain", "Marketplace"],
        },
    ];
}
