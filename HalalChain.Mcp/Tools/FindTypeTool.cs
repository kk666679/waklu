using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>
/// Read-only Wave-1 tool: locate any type declaration by name. The only tool
/// in this group that requires an argument, so it is also the one that
/// publishes an <c>isError</c> result when the argument is missing.
/// </summary>
public sealed class FindTypeTool : ITool
{
    private readonly ICodeIntrospectionService _introspection;

    public FindTypeTool(ICodeIntrospectionService introspection)
    {
        _introspection = introspection;
    }

    public string Name => "halalchain_find_type";

    public string Description =>
        "Locates a type declaration (class, record, struct, interface, enum) by name. Exact matches are listed first, then substring matches.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            name = new { type = "string", description = "Type name to find, e.g. `Vendor` or `IValueObject`" },
            project = new { type = "string", description = "Optional owning project name" }
        },
        required = new[] { "name" }
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("Find a type declaration");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default) =>
        Task.FromResult(Render(arguments));

    public Task<CallToolResult> ExecuteDetailedAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var missing = ToolArguments.String(arguments, "name") is null;
        return Task.FromResult(new CallToolResult
        {
            IsError = missing ? true : null,
            Content = [ContentItem.FromText(Render(arguments))],
        });
    }

    private string Render(JsonElement arguments)
    {
        var name = ToolArguments.String(arguments, "name");
        if (name is null)
        {
            return "**Error:** `name` is required — pass the type to find, e.g. `{ \"name\": \"Vendor\" }`.\n";
        }

        var matches = _introspection.FindType(name, ToolArguments.String(arguments, "project"));
        var output = $"# Type Matches for `{name}` ({matches.Count})\n\n";

        if (matches.Count == 0)
        {
            return output +
                   $"No declaration matched. The scan walks the solution root — check `{HalalChainOptions.GetSolutionRootEnvVar()}` " +
                   "if this should be non-empty.\n";
        }

        foreach (var match in matches)
        {
            output += $"- **{match.Name}** ({match.Kind})\n" +
                      $"  `{match.RelativePath}:{match.Line}` ({match.ProjectName})\n";
        }

        output += "\nDetection: declaration keywords (`class`, `record`, `struct`, `interface`, `enum`) with modifiers, " +
                  "primary constructors, and base lists captured as written. Matching is name-only — it reports where a type " +
                  "is declared, never what it means.\n";
        return output;
    }
}