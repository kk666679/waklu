using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-1 tool: HTTP endpoints discovered in the sources.</summary>
public sealed class ListEndpointsTool : ITool
{
    private readonly ICodeIntrospectionService _introspection;

    public ListEndpointsTool(ICodeIntrospectionService introspection)
    {
        _introspection = introspection;
    }

    public string Name => "halalchain_list_endpoints";

    public string Description =>
        "Lists HTTP endpoints discovered from [HttpVerb] attributes on [ApiController] classes, with class-level routes composed in.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional owning project name (e.g. HalalChain.Platform.Api)" }
        },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List HTTP endpoints");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var endpoints = _introspection.GetEndpoints(ToolArguments.String(arguments, "project"));
        var output = $"# HTTP Endpoints ({endpoints.Count})\n\n";

        if (endpoints.Count == 0)
        {
            return Task.FromResult(
                output +
                $"Nothing found. The scan walks the solution root — check `{HalalChainOptions.GetSolutionRootEnvVar()}` " +
                "if this should be non-empty.\n");
        }

        foreach (var group in endpoints.GroupBy(e => e.ProjectName).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            output += $"## {group.Key}\n\n";
            foreach (var endpoint in group)
            {
                output += $"- **{endpoint.Verb} {endpoint.Path}** → `{endpoint.Handler}` ({endpoint.Controller})\n" +
                          $"  `{endpoint.RelativePath}:{endpoint.Line}`\n";
            }

            output += "\n";
        }

        output += "Detection: `[HttpVerb]` attributes inside classes marked `[ApiController]`; the class `[Route]` is composed with the action template.\n";
        return Task.FromResult(output);
    }
}