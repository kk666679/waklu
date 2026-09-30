using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-1 tool: aggregate roots and entities.</summary>
public sealed class ListAggregateRootsTool : ITool
{
    private readonly ICodeIntrospectionService _introspection;

    public ListAggregateRootsTool(ICodeIntrospectionService introspection)
    {
        _introspection = introspection;
    }

    public string Name => "halalchain_list_aggregate_roots";

    public string Description =>
        "Lists aggregate roots and entities detected by the solution's IAggregateRoot, AggregateRoot, and Entity base conventions.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional owning project name" }
        },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List aggregate roots");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var roots = _introspection.GetAggregateRoots(ToolArguments.String(arguments, "project"));
        var output = $"# Aggregate Roots ({roots.Count})\n\n";

        if (roots.Count == 0)
        {
            return Task.FromResult(
                output +
                $"Nothing found. The scan walks the solution root — check `{HalalChainOptions.GetSolutionRootEnvVar()}` " +
                "if this should be non-empty.\n");
        }

        foreach (var group in roots.GroupBy(r => r.Kind).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            output += $"## {group.Key} ({group.Count()})\n\n";
            foreach (var root in group)
            {
                output += $"- **{root.Name}**\n  `{root.RelativePath}:{root.Line}` ({root.ProjectName})\n";
            }

            output += "\n";
        }

        output += "Detection: the declaration's base list (not the file's comments) names `IAggregateRoot`, " +
                  "`AggregateRoot`, or an `Entity` base type.\n";
        return Task.FromResult(output);
    }
}