using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Abstractions;

public interface IPlatformDataService
{
    string GetSolutionRoot();
    IReadOnlyList<ProjectInfo> GetProjects();
    IReadOnlyList<PageInfo> GetPages(string? projectName = null);
    IReadOnlyList<ComponentInfo> GetComponents(string? projectName = null);
    IReadOnlyList<ServiceInfoMc> GetServices(string? projectName = null);
    IReadOnlyList<ControllerInfo> GetControllers(string? projectName = null);
    IReadOnlyList<ViewInfo> GetViews(string? projectName = null);
    PlatformOverviewMc GetPlatformOverview();
    string GetArchitecture();
}
