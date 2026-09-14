namespace HalalChain.Mcp.Models;

public class ProjectInfo
{
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string TargetFramework { get; set; } = "";
    public string ProjectKind { get; set; } = "";
    public List<string> PackageReferences { get; set; } = [];
    public List<string> ProjectReferences { get; set; } = [];
}

public class PageInfo
{
    public string Name { get; set; } = "";
    public string? Route { get; set; }
    public string RelativePath { get; set; } = "";
    public string Directory { get; set; } = "";
    public string? Layout { get; set; }
    public string ProjectName { get; set; } = "";
}

public class ComponentInfo
{
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string Directory { get; set; } = "";
    public List<string> Parameters { get; set; } = [];
    public string ProjectName { get; set; } = "";
}

public class ServiceInfoMc
{
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public List<string> Interfaces { get; set; } = [];
    public List<string> Methods { get; set; } = [];
    public string ProjectName { get; set; } = "";
}

public class ControllerInfo
{
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public List<string> Routes { get; set; } = [];
    public string ProjectName { get; set; } = "";
}

public class ViewInfo
{
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string Directory { get; set; } = "";
    public string? Layout { get; set; }
    public string ProjectName { get; set; } = "";
}

public class PlatformOverviewMc
{
    public string SolutionName { get; set; } = "";
    public string DotnetVersion { get; set; } = "";
    public int TotalProjects { get; set; }
    public int TotalPages { get; set; }
    public int TotalComponents { get; set; }
    public int TotalServices { get; set; }
    public int TotalControllers { get; set; }
    public int TotalViews { get; set; }
    public List<ProjectInfo> Projects { get; set; } = [];
    public List<PageInfo> Pages { get; set; } = [];
    public List<ComponentInfo> Components { get; set; } = [];
    public List<ServiceInfoMc> Services { get; set; } = [];
    public List<ControllerInfo> Controllers { get; set; } = [];
    public List<ViewInfo> Views { get; set; } = [];
}

public class HealthCheckResult
{
    public string Service { get; set; } = "";
    public string Url { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsHealthy { get; set; }
}
