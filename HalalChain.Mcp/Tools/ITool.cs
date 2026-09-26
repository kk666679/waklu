using System.Text.Json;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

public interface ITool
{
    string Name { get; }

    string Description { get; }

    object InputSchema { get; }

    /// <summary>
    /// Behavioural hints for the host (MCP 2025-03-26+). Every tool in this
    /// server only reads, so the safe default is a local read-only tool; tools
    /// that touch running services override this with
    /// <see cref="ToolAnnotations.NetworkReadOnly"/>.
    /// </summary>
    ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly(Name);

    /// <summary>
    /// JSON Schema for the structured result, when the tool publishes one.
    /// Null means the tool returns text only.
    /// </summary>
    object? OutputSchema => null;

    /// <summary>
    /// Renders the tool as Markdown. This is the original text-only MCP
    /// contract and stays the projection used for older clients.
    /// </summary>
    Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default);

    /// <summary>
    /// Executes the tool and returns a full <c>tools/call</c> result with a
    /// content-block array. The default wraps <see cref="ExecuteAsync"/>, so a
    /// tool only overrides this when it has structured data to publish.
    /// </summary>
    async Task<CallToolResult> ExecuteDetailedAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var markdown = await ExecuteAsync(arguments, ct).ConfigureAwait(false);
        return new CallToolResult { Content = [ContentItem.FromText(markdown)] };
    }
}
