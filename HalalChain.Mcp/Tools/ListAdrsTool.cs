using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-2 tool: Architecture Decision Records index.</summary>
public sealed class ListAdrsTool : ITool
{
    private readonly IGovernanceService _governance;

    public ListAdrsTool(IGovernanceService governance)
    {
        _governance = governance;
    }

    public string Name => "halalchain_list_adrs";

    public string Description =>
        "Lists Architecture Decision Records with their ids, titles, and file paths.";

    public object InputSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List architecture decision records");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var index = _governance.ListAdrs();
        var output = $"# Architecture Decision Records ({index.Adrs.Count})\n\n";

        if (index.Adrs.Count == 0)
        {
            output += index.Directory is null
                ? "No ADR directory exists yet. Searched:\n\n"
                : $"Directory `{index.Directory}` exists but holds no `.md` files. Searched:\n\n";
            foreach (var candidate in index.Searched)
            {
                output += $"- `{candidate}`\n";
            }

            return Task.FromResult(output);
        }

        output += $"Directory: `{index.Directory}`\n\n";
        foreach (var adr in index.Adrs)
        {
            output += $"- **{adr.Id}** — {adr.Title}\n  `{adr.RelativePath}`\n";
        }

        return Task.FromResult(output);
    }
}