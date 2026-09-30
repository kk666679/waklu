using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-2 tool: read one ADR by id.</summary>
public sealed class GetAdrTool : ITool
{
    private readonly IGovernanceService _governance;

    public GetAdrTool(IGovernanceService governance)
    {
        _governance = governance;
    }

    public string Name => "halalchain_get_adr";

    public string Description =>
        "Reads one Architecture Decision Record by id (e.g. 0001) and returns its full Markdown.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            id = new { type = "string", description = "ADR id: alphanumeric with optional dashes or underscores (e.g. 0001)" }
        },
        required = new[] { "id" }
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("Read one ADR");

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
        _governance.GetAdr(ToolArguments.String(arguments, "id") ?? "");

    private static string Render(DocumentLookup lookup)
    {
        if (lookup.Error is not null)
        {
            return $"**Error:** {lookup.Error}\n\nPass an id such as `0001`.\n";
        }

        if (!lookup.Found || lookup.Content is null)
        {
            var output = "No matching ADR. Searched:\n\n";
            foreach (var candidate in lookup.Searched)
            {
                output += $"- `{candidate}`\n";
            }

            return output;
        }

        return $"Source: `{lookup.RelativePath}`\n\n{lookup.Content}";
    }
}