using System.Text.Json;

namespace HalalChain.Mcp.Tools;

/// <summary>
/// Argument readers shared by the introspection tools. A missing or blank
/// argument yields null rather than an exception: an exception from a tool
/// surfaces as a JSON-RPC internal error, while null lets the tool phrase the
/// "you forgot the argument" answer in its own output.
/// </summary>
internal static class ToolArguments
{
    public static string? String(JsonElement arguments, string name)
    {
        if (arguments.ValueKind == JsonValueKind.Object &&
            arguments.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        return null;
    }
}