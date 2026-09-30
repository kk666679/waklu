using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-2 tool: the platform service manifest.</summary>
public sealed class GetServiceManifestTool : ITool
{
    private readonly IGovernanceService _governance;

    public GetServiceManifestTool(IGovernanceService governance)
    {
        _governance = governance;
    }

    public string Name => "halalchain_get_service_manifest";

    public string Description =>
        "Reads service-manifest.yaml — the inventory of platform services, ports, and their roles.";

    public object InputSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("Read the service manifest");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var lookup = _governance.GetServiceManifest();
        var output = "# Service Manifest\n\n";

        if (!lookup.Found || lookup.Content is null)
        {
            output += "No service manifest was found. Searched:\n\n";
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