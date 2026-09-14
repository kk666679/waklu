using System.Text.Json;
using HalalChain.Mcp.Abstractions;

namespace HalalChain.Mcp.Tools;

public sealed class ListComponentsTool : ITool
{
    private readonly IPlatformDataService _platformData;

    public ListComponentsTool(IPlatformDataService platformData)
    {
        _platformData = platformData;
    }

    public string Name => "halalchain_list_components";

    public string Description => "Lists all Blazor components across HalalChain projects with their parameters and owning project.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional project name to filter components (e.g. HalalChain, HalalChain.Marketplace)" }
        },
        required = Array.Empty<string>()
    };

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var projectName = arguments.ValueKind == JsonValueKind.Object &&
                          arguments.TryGetProperty("project", out var p) &&
                          p.ValueKind == JsonValueKind.String
                          ? p.GetString()
                          : null;

        var components = _platformData.GetComponents(projectName);
        var output = $"# Blazor Components ({components.Count} total)\n\n";

        var grouped = components.GroupBy(c => c.ProjectName).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            output += $"## {group.Key}\n\n";
            var dirGroups = group.GroupBy(c => c.Directory).OrderBy(g => g.Key);
            foreach (var dirGroup in dirGroups)
            {
                output += $"### {dirGroup.Key}\n\n";
                foreach (var c in dirGroup)
                {
                    output += $"- **{c.Name}**";
                    if (c.Parameters.Any())
                        output += $" — params: {string.Join(", ", c.Parameters)}";
                    output += $"\n  `{c.RelativePath}`\n";
                }
                output += "\n";
            }
        }

        return Task.FromResult(output);
    }
}
