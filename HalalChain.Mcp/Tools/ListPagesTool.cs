using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

public sealed class ListPagesTool : ITool
{
    private readonly IPlatformDataService _platformData;

    public ListPagesTool(IPlatformDataService platformData)
    {
        _platformData = platformData;
    }

    public string Name => "halalchain_list_pages";

    public string Description => "Lists all Razor pages across HalalChain projects with their routes, layouts, and owning project.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional project name to filter pages (e.g. HalalChain, HalalChain.Marketplace)" }
        },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List Razor pages");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var filterProject = arguments.ValueKind == JsonValueKind.Object &&
                            arguments.TryGetProperty("project", out var projArg) &&
                            projArg.ValueKind == JsonValueKind.String
                            ? projArg.GetString()
                            : null;

        var pages = _platformData.GetPages(filterProject);
        var output = $"# Razor Pages ({pages.Count} total)\n\n";

        var grouped = pages.GroupBy(p => p.ProjectName).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            output += $"## {group.Key}\n\n";
            var dirGroups = group.GroupBy(p => p.Directory).OrderBy(g => g.Key);
            foreach (var dirGroup in dirGroups)
            {
                output += $"### {dirGroup.Key}\n\n";
                foreach (var page in dirGroup)
                {
                    output += $"- **{page.Name}** → `{page.Route}`";
                    if (page.Layout != null) output += $" (layout: {page.Layout})";
                    output += $"\n  `{page.RelativePath}`\n";
                }
                output += "\n";
            }
        }

        return Task.FromResult(output);
    }
}
