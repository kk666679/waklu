using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-2 tool: the numbered principles document.</summary>
public sealed class GetPrinciplesTool : ITool
{
    private readonly IGovernanceService _governance;

    public GetPrinciplesTool(IGovernanceService governance)
    {
        _governance = governance;
    }

    public string Name => "halalchain_get_principles";

    public string Description =>
        "Parses the numbered principles (P1, P2, …) from the first principles document found, with the line each one is declared on.";

    public object InputSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("Read governance principles");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var document = _governance.GetPrinciples();
        var output = "# Governance Principles\n\n";

        if (document.Source is null)
        {
            output += "No principles document was found. Searched:\n\n";
            foreach (var candidate in document.Searched)
            {
                output += $"- `{candidate}`\n";
            }

            return Task.FromResult(output);
        }

        output += $"Source: `{document.Source}` ({document.Principles.Count} principles)\n\n";

        foreach (var principle in document.Principles)
        {
            output += $"- **{principle.Id}** — {principle.Title}  \n  `{document.Source}:{principle.Line}`\n";
        }

        output += "\nReading the principles states what the platform is bound by. It does not evaluate anyone against them — " +
                  "that is the deterministic Policy Engine's job.\n";
        return Task.FromResult(output);
    }
}