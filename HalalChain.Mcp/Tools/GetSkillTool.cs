using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-2 tool: read one skill by slug.</summary>
public sealed class GetSkillTool : ITool
{
    private readonly IGovernanceService _governance;

    public GetSkillTool(IGovernanceService governance)
    {
        _governance = governance;
    }

    public string Name => "halalchain_get_skill";

    public string Description =>
        "Reads one skill's manifest or steering document by slug and returns its full Markdown.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            slug = new { type = "string", description = "Skill slug: alphanumeric with optional dashes or underscores" }
        },
        required = new[] { "slug" }
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("Read one skill");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default) =>
        Task.FromResult(Render(Lookup(arguments)));

    public Task<CallToolResult> ExecuteDetailedAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var lookup = Lookup(arguments);
        return Task.FromResult(new CallToolResult
        {
            IsError = lookup.Error is null ? null : true,
            Content = [ContentItem.FromText(Render(lookup))],
        });
    }

    private DocumentLookup Lookup(JsonElement arguments) =>
        _governance.GetSkill(ToolArguments.String(arguments, "slug") ?? "");

    private static string Render(DocumentLookup lookup)
    {
        if (lookup.Error is not null)
        {
            return $"**Error:** {lookup.Error}\n\nPass a slug such as `doc-writer`.\n";
        }

        if (!lookup.Found || lookup.Content is null)
        {
            var output = "No matching skill. Searched:\n\n";
            foreach (var candidate in lookup.Searched)
            {
                output += $"- `{candidate}`\n";
            }

            return output;
        }

        return $"Source: `{lookup.RelativePath}`\n\n{lookup.Content}";
    }
}