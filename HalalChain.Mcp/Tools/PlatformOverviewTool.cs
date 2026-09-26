using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

public sealed class PlatformOverviewTool : ITool
{
    private readonly IPlatformDataService _platformData;

    public PlatformOverviewTool(IPlatformDataService platformData)
    {
        _platformData = platformData;
    }

    public string Name => "halalchain_platform_overview";

    public string Description => "Returns a comprehensive HalalChain platform overview including all projects, pages, components, services, controllers, views, and statistics.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional project name to filter overview (e.g. HalalChain, HalalChain.Marketplace)" }
        },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("HalalChain platform overview");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var projectName = arguments.ValueKind == JsonValueKind.Object &&
                          arguments.TryGetProperty("project", out var p) &&
                          p.ValueKind == JsonValueKind.String
                          ? p.GetString()
                          : null;

        var overview = projectName == null
            ? _platformData.GetPlatformOverview()
            : new PlatformOverviewMc
            {
                SolutionName = _platformData.GetPlatformOverview().SolutionName,
                DotnetVersion = "10.0",
                TotalProjects = 1,
                TotalPages = _platformData.GetPages(projectName).Count,
                TotalComponents = _platformData.GetComponents(projectName).Count,
                TotalServices = _platformData.GetServices(projectName).Count,
                TotalControllers = _platformData.GetControllers(projectName).Count,
                TotalViews = _platformData.GetViews(projectName).Count,
                Projects = _platformData.GetProjects().Where(p => p.Name == projectName).ToList(),
                Pages = _platformData.GetPages(projectName).ToList(),
                Components = _platformData.GetComponents(projectName).ToList(),
                Services = _platformData.GetServices(projectName).ToList(),
                Controllers = _platformData.GetControllers(projectName).ToList(),
                Views = _platformData.GetViews(projectName).ToList(),
            };

        var output = $"""
            # HalalChain Platform Overview

            ## Summary
            | Metric | Count |
            |--------|-------|
            | Projects | {overview.TotalProjects} |
            | Razor Pages | {overview.TotalPages} |
            | Components | {overview.TotalComponents} |
            | Services | {overview.TotalServices} |
            | Controllers | {overview.TotalControllers} |
            | Views | {overview.TotalViews} |

            ## Projects ({overview.TotalProjects})
            """;

        foreach (var proj in overview.Projects)
            output += $"\n- **{proj.Name}** ({proj.TargetFramework}) — `{proj.RelativePath}` [{proj.ProjectKind}]";

        output += "\n\n## Pages by Project\n";
        var pageGroups = overview.Pages.GroupBy(p => p.ProjectName).OrderBy(g => g.Key);
        foreach (var group in pageGroups)
        {
            output += $"\n### {group.Key} ({group.Count()})\n";
            foreach (var page in group)
                output += $"\n- `{page.Route}` → {page.Name}";
        }

        output += "\n\n## Components by Project\n";
        var compGroups = overview.Components.GroupBy(c => c.ProjectName).OrderBy(g => g.Key);
        foreach (var group in compGroups)
        {
            output += $"\n### {group.Key} ({group.Count()})\n";
            foreach (var c in group)
                output += $"\n- **{c.Name}** — {string.Join(", ", c.Parameters)}";
        }

        output += "\n\n## Services by Project\n";
        var svcGroups = overview.Services.GroupBy(s => s.ProjectName).OrderBy(g => g.Key);
        foreach (var group in svcGroups)
        {
            output += $"\n### {group.Key} ({group.Count()})\n";
            foreach (var s in group)
                output += $"\n- **{s.Name}** — {string.Join(", ", s.Interfaces)}";
        }

        output += "\n\n## Controllers by Project\n";
        var ctrlGroups = overview.Controllers.GroupBy(c => c.ProjectName).OrderBy(g => g.Key);
        foreach (var group in ctrlGroups)
        {
            output += $"\n### {group.Key} ({group.Count()})\n";
            foreach (var c in group)
            {
                output += $"\n- **{c.Name}**";
                if (c.Routes.Any())
                    output += $" — routes: {string.Join(", ", c.Routes.Take(5))}";
            }
        }

        output += "\n\n## Views by Project\n";
        var viewGroups = overview.Views.GroupBy(v => v.ProjectName).OrderBy(g => g.Key);
        foreach (var group in viewGroups)
        {
            output += $"\n### {group.Key} ({group.Count()})\n";
            foreach (var v in group)
                output += $"\n- **{v.Name}** — `{v.RelativePath}`";
        }

        return Task.FromResult(output);
    }
}
