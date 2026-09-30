using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-2 tool: the controls manifest.</summary>
public sealed class GetControlsTool : ITool
{
    private readonly IGovernanceService _governance;

    public GetControlsTool(IGovernanceService governance)
    {
        _governance = governance;
    }

    public string Name => "halalchain_get_controls";

    public string Description =>
        "Reads the controls manifest when the repository has one, returning its full Markdown and every path probed.";

    public object InputSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("Read the controls manifest");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var lookup = _governance.GetControls();
        var output = "# Controls Manifest\n\n";

        if (!lookup.Found || lookup.Content is null)
        {
            output += "No controls manifest was found. Searched:\n\n";
            foreach (var candidate in lookup.Searched)
            {
                output += $"- `{candidate}`\n";
            }

            return Task.FromResult(output);
        }

        output += $"Source: `{lookup.RelativePath}`\n\n{lookup.Content}";
        return Task.FromResult(output);
    }
}