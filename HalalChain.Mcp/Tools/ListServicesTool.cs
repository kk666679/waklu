using System.Text.Json;
using HalalChain.Mcp.Abstractions;

namespace HalalChain.Mcp.Tools;

public sealed class ListServicesTool : ITool
{
    private readonly IPlatformDataService _platformData;

    public ListServicesTool(IPlatformDataService platformData)
    {
        _platformData = platformData;
    }

    public string Name => "halalchain_list_services";

    public string Description => "Lists all C# service classes across HalalChain projects with their interfaces, methods, and owning project.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional project name to filter services (e.g. HalalChain, HalalChain.Marketplace)" }
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

        var services = _platformData.GetServices(projectName);
        var output = $"# Services ({services.Count} files)\n\n";

        var grouped = services.GroupBy(s => s.ProjectName).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            output += $"## {group.Key}\n\n";
            foreach (var s in group)
            {
                output += $"### {s.Name}\n";
                output += $"- **Path**: `{s.RelativePath}`\n";
                if (s.Interfaces.Any())
                    output += $"- **Interfaces**: {string.Join(", ", s.Interfaces)}\n";
                if (s.Methods.Any())
                {
                    output += "- **Public Methods**:\n";
                    foreach (var m in s.Methods.Take(10))
                        output += $"  - `{m}`\n";
                    if (s.Methods.Count > 10)
                        output += $"  - ... and {s.Methods.Count - 10} more\n";
                }
                output += "\n";
            }
        }

        return Task.FromResult(output);
    }
}
