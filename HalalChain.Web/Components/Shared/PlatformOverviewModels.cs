namespace HalalChain.Components.Shared;

public class PlatformOverviewData
{
    public List<ServiceInfo> Services { get; set; } = new();
    public List<string> VerificationFlowSteps { get; set; } = new();
    public string ArchitectureDiagram { get; set; } = string.Empty;
    public List<HealthEndpointInfo> HealthEndpoints { get; set; } = new();
    public int TotalServices => Services.Count;
    public int RunningServices => Services.Count(s => s.Status == ServiceStatus.Running);
    public int DownServices => Services.Count(s => s.Status == ServiceStatus.Stopped);
}

public class ServiceInfo
{
    public string Name { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string? HealthUrl { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
}

public class HealthEndpointInfo
{
    public string Endpoint { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Services { get; set; } = new();
}

public enum ServiceStatus
{
    Unknown,
    Running,
    Stopped,
}
