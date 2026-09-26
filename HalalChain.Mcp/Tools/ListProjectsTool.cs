using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

public sealed class ListProjectsTool : ITool
{
    private readonly IPlatformDataService _platformData;

    public ListProjectsTool(IPlatformDataService platformData)
    {
        _platformData = platformData;
    }

    public string Name => "halalchain_list_projects";

    public string Description => "Lists all .NET projects in the HalalChain solution with their target frameworks and references.";

    public object InputSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List .NET projects");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var projects = _platformData.GetProjects();
        var output = "# HalalChain Projects\n\n";

        foreach (var p in projects)
        {
            output += $"## {p.Name}\n";
            output += $"- **Path**: `{p.RelativePath}`\n";
            output += $"- **Target**: {p.TargetFramework}\n";
            output += $"- **Kind**: {p.ProjectKind}\n";
            if (p.PackageReferences.Any())
                output += $"- **Packages**: {string.Join(", ", p.PackageReferences)}\n";
            if (p.ProjectReferences.Any())
                output += $"- **References**: {string.Join(", ", p.ProjectReferences)}\n";
            output += "\n";
        }

        return Task.FromResult(output);
    }
}
