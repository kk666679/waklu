using System.Text.Json;
using HalalChain.Mcp.Abstractions;

namespace HalalChain.Mcp.Tools;

public sealed class ProjectStatusTool : ITool
{
    private readonly IPlatformDataService _platformData;

    public ProjectStatusTool(IPlatformDataService platformData)
    {
        _platformData = platformData;
    }

    public string Name => "halalchain_project_status";

    public string Description => "Returns overall HalalChain project status including project count, pages, components, services, controllers, views, and solution info.";

    public object InputSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var overview = _platformData.GetPlatformOverview();
        var projects = _platformData.GetProjects();

        var output = $"""
            # HalalChain Project Status

            ## Solution
            - **Name**: {overview.SolutionName}
            - **.NET Version**: {overview.DotnetVersion}
            - **Projects**: {overview.TotalProjects}
            - **Razor Pages**: {overview.TotalPages}
            - **Blazor Components**: {overview.TotalComponents}
            - **Services**: {overview.TotalServices}
            - **Controllers**: {overview.TotalControllers}
            - **Views**: {overview.TotalViews}

            ## Projects
            """;

        foreach (var p in projects)
        {
            output += $"\n- **{p.Name}** ({p.TargetFramework}) — `{p.RelativePath}` [{p.ProjectKind}]";
            if (p.ProjectReferences.Any())
                output += $"\n  References: {string.Join(", ", p.ProjectReferences)}";
        }

        return Task.FromResult(output);
    }
}
