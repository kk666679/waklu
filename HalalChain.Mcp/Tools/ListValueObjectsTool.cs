using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-1 tool: value objects.</summary>
public sealed class ListValueObjectsTool : ITool
{
    private readonly ICodeIntrospectionService _introspection;

    public ListValueObjectsTool(ICodeIntrospectionService introspection)
    {
        _introspection = introspection;
    }

    public string Name => "halalchain_list_value_objects";

    public string Description =>
        "Lists value objects: records declared in a ValueObjects file or folder, or records implementing IValueObject.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional owning project name" }
        },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List value objects");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var valueObjects = _introspection.GetValueObjects(ToolArguments.String(arguments, "project"));
        var output = $"# Value Objects ({valueObjects.Count})\n\n";

        if (valueObjects.Count == 0)
        {
            return Task.FromResult(
                output +
                $"Nothing found. The scan walks the solution root — check `{HalalChainOptions.GetSolutionRootEnvVar()}` " +
                "if this should be non-empty.\n");
        }

        foreach (var group in valueObjects.GroupBy(v => v.ProjectName).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            output += $"## {group.Key}\n\n";
            foreach (var valueObject in group)
            {
                output += $"- **{valueObject.Name}** — {valueObject.Kind}\n" +
                          $"  `{valueObject.RelativePath}:{valueObject.Line}`\n";
            }

            output += "\n";
        }

        output += "Detection: a `record` counts when it implements `IValueObject`, or when it is declared inside a " +
                  "`ValueObjects.cs` file or a `ValueObjects/` folder.\n";
        return Task.FromResult(output);
    }
}