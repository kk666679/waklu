using System.Text.Json;
using System.Text.Json.Serialization;

namespace HalalChain.Mcp.Models;

/// <summary>
/// Single source of truth for MCP wire serialization. Every outbound frame —
/// responses, batch responses, notifications, and server-initiated requests —
/// goes through these options so casing and null handling stay consistent
/// across the transport.
/// </summary>
public static class McpJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Resource payloads embed arbitrary repository text (razor, C#, Solidity).
        // Escaping the non-ASCII and HTML-sensitive characters by default keeps
        // those payloads byte-safe on the stdio transport.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
