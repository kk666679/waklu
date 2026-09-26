using System.Text.Json;
using System.Text.Json.Serialization;

namespace HalalChain.Mcp.Models;

// ──────────────────────────────────────────────────────────────────────────
// JSON-RPC 2.0 wire models.
//
// The server speaks the MCP transport (stdio + JSON-RPC 2.0 framing) and
// negotiates the MCP protocol version on every `initialize` handshake.
// We advertise the latest spec version we support (2025-06-18) and fall
// back to the client's requested version when it is older.
// ──────────────────────────────────────────────────────────────────────────

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }

    private string _method = "";

    /// <summary>
    /// Request method. Coalesced on assignment so an explicit
    /// <c>"method": null</c> cannot turn into a null dereference on the
    /// dispatch path.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method
    {
        get => _method;
        set => _method = value ?? "";
    }
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}