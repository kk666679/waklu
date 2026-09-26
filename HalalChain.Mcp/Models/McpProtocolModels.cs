using System.Text.Json.Serialization;

namespace HalalChain.Mcp.Models;

// ──────────────────────────────────────────────────────────────────────────
// MCP protocol surface (spec revision 2025-06-18).
//
// The 2025-06-18 revision is the first one that treats tool *annotations*,
// *structured* tool output, and the bidirectional primitives (sampling,
// elicitation) as first-class. Everything in this file exists to serve that
// revision; the legacy text-only shapes remain valid projections of it, so
// older clients that predate structured output still work unchanged.
// ──────────────────────────────────────────────────────────────────────────

/// <summary>
/// Version negotiation for the MCP handshake. The server advertises the
/// newest revision it understands and honours an older revision when the
/// client explicitly asks for one.
/// </summary>
public static class McpProtocol
{
    /// <summary>Newest revision implemented by this server.</summary>
    public const string Latest = "2025-06-18";

    /// <summary>Revisions this server can speak, newest first.</summary>
    public static readonly string[] Supported =
    [
        "2025-06-18",
        "2025-03-26",
        "2024-11-05",
    ];

    /// <summary>
    /// Resolves the revision to use for the rest of the session. A client
    /// request for a revision we implement wins; anything else (including a
    /// missing or future revision) falls back to <see cref="Latest"/>.
    /// </summary>
    public static string Negotiate(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return Latest;
        }

        return Array.IndexOf(Supported, requested) >= 0 ? requested : Latest;
    }

    /// <summary>True when the negotiated revision predates structured tool output.</summary>
    public static bool SupportsStructuredContent(string negotiated) =>
        string.CompareOrdinal(negotiated, "2025-06-18") >= 0;

    /// <summary>True when the negotiated revision understands tool annotations.</summary>
    public static bool SupportsAnnotations(string negotiated) =>
        string.CompareOrdinal(negotiated, "2025-03-26") >= 0;
}

// ── Capabilities ──────────────────────────────────────────────────────────

/// <summary>Capabilities the *client* declared during <c>initialize</c>.</summary>
public sealed class ClientCapabilities
{
    [JsonPropertyName("roots")] public bool? Roots { get; set; }
    [JsonPropertyName("sampling")] public bool? Sampling { get; set; }
    [JsonPropertyName("elicitation")] public bool? Elicitation { get; set; }
    [JsonPropertyName("tools")] public bool? Tools { get; set; }
    [JsonPropertyName("resources")] public bool? Resources { get; set; }
    [JsonPropertyName("prompts")] public bool? Prompts { get; set; }
    [JsonPropertyName("logging")] public bool? Logging { get; set; }
    [JsonPropertyName("completions")] public bool? Completions { get; set; }
}

// ── Tool annotations (2025-03-26+) ───────────────────────────────────────

/// <summary>
/// Behavioural hints attached to a tool. These are advisory: a host uses them
/// to decide whether to auto-approve a call. Every tool in this server is
/// read-only, so the hints are uniform apart from <c>openWorldHint</c>, which
/// distinguishes local repository inspection from live network probing.
/// </summary>
public sealed class ToolAnnotations
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("readOnlyHint")] public bool ReadOnlyHint { get; set; }
    [JsonPropertyName("destructiveHint")] public bool DestructiveHint { get; set; }
    [JsonPropertyName("idempotentHint")] public bool IdempotentHint { get; set; }
    [JsonPropertyName("openWorldHint")] public bool OpenWorldHint { get; set; }

    /// <summary>Read-only over the local repository: no writes, no network.</summary>
    public static ToolAnnotations LocalReadOnly(string? title = null) => new()
    {
        Title = title,
        ReadOnlyHint = true,
        DestructiveHint = false,
        IdempotentHint = true,
        OpenWorldHint = false,
    };

    /// <summary>Read-only but reaches out to running services over the network.</summary>
    public static ToolAnnotations NetworkReadOnly(string? title = null) => new()
    {
        Title = title,
        ReadOnlyHint = true,
        DestructiveHint = false,
        IdempotentHint = true,
        OpenWorldHint = true,
    };
}

// ── Content blocks ────────────────────────────────────────────────────────

/// <summary>
/// A single MCP content block. The 2025-06-18 revision added <c>resource</c>
/// and <c>audio</c> alongside the original <c>text</c> and <c>image</c>.
/// </summary>
public sealed class ContentItem
{
    [JsonPropertyName("type")] public string Type { get; set; } = "text";
    [JsonPropertyName("text")] public string? Text { get; set; }
    [JsonPropertyName("mimeType")] public string? MimeType { get; set; }
    [JsonPropertyName("data")] public string? Data { get; set; }
    [JsonPropertyName("uri")] public string? Uri { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("resource")] public ResourceContents? Resource { get; set; }

    public static ContentItem FromText(string text) => new() { Type = "text", Text = text };

    public static ContentItem FromEmbedded(ResourceContents contents) =>
        new() { Type = "resource", Resource = contents };

    public static ContentItem FromLink(ResourceDefinition definition) => new()
    {
        Type = "resource_link",
        Uri = definition.Uri,
        Name = definition.Name,
        MimeType = definition.MimeType,
        Description = definition.Description,
    };
}

/// <summary>Resource payload returned by <c>resources/read</c>.</summary>
public sealed class ResourceContents
{
    [JsonPropertyName("uri")] public string Uri { get; set; } = "";
    [JsonPropertyName("mimeType")] public string? MimeType { get; set; }
    [JsonPropertyName("text")] public string? Text { get; set; }
    [JsonPropertyName("blob")] public string? Blob { get; set; }
}

/// <summary>Resource descriptor returned by <c>resources/list</c>.</summary>
public sealed class ResourceDefinition
{
    [JsonPropertyName("uri")] public string Uri { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("mimeType")] public string? MimeType { get; set; }
    [JsonPropertyName("size")] public long? Size { get; set; }
}

/// <summary>
/// Result of <c>tools/call</c>. <c>structuredContent</c> is only emitted when
/// the client negotiated a revision that understands it.
/// </summary>
public sealed class CallToolResult
{
    [JsonPropertyName("content")] public List<ContentItem> Content { get; set; } = [];
    [JsonPropertyName("structuredContent")] public object? StructuredContent { get; set; }
    [JsonPropertyName("isError")] public bool? IsError { get; set; }
}

// ── Prompts ───────────────────────────────────────────────────────────────

public sealed class PromptArgument
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("required")] public bool? Required { get; set; }
}

public sealed class PromptDefinition
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("arguments")] public List<PromptArgument> Arguments { get; set; } = [];
}

public sealed class PromptMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = "user";
    [JsonPropertyName("content")] public ContentItem Content { get; set; } = new();
}

public sealed class GetPromptResult
{
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("messages")] public List<PromptMessage> Messages { get; set; } = [];
}

// ── Sampling (server → client LLM call) ───────────────────────────────────

public sealed class SamplingMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = "user";
    [JsonPropertyName("content")] public ContentItem Content { get; set; } = new();
}

public sealed class CreateMessageRequest
{
    [JsonPropertyName("messages")] public List<SamplingMessage> Messages { get; set; } = [];
    [JsonPropertyName("maxTokens")] public int MaxTokens { get; set; } = 1024;
    [JsonPropertyName("systemPrompt")] public string? SystemPrompt { get; set; }
    [JsonPropertyName("temperature")] public double? Temperature { get; set; }
}

public sealed class CreateMessageResult
{
    [JsonPropertyName("role")] public string Role { get; set; } = "assistant";
    [JsonPropertyName("content")] public ContentItem Content { get; set; } = new();
    [JsonPropertyName("model")] public string? Model { get; set; }
    [JsonPropertyName("stopReason")] public string? StopReason { get; set; }
}

// ── Elicitation (server → client user input) ──────────────────────────────

public static class ElicitAction
{
    public const string Accept = "accept";
    public const string Decline = "decline";
    public const string Cancel = "cancel";
}

public sealed class ElicitResult
{
    [JsonPropertyName("action")] public string Action { get; set; } = ElicitAction.Decline;
    [JsonPropertyName("content")] public Dictionary<string, System.Text.Json.JsonElement>? Content { get; set; }

    public bool Accepted => string.Equals(Action, ElicitAction.Accept, StringComparison.Ordinal);
}

// ── Progress notifications ────────────────────────────────────────────────

public sealed class ProgressNotification
{
    [JsonPropertyName("progressToken")] public string ProgressToken { get; set; } = "";
    [JsonPropertyName("progress")] public double Progress { get; set; }
    [JsonPropertyName("total")] public double? Total { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}

// ── Logging channel ───────────────────────────────────────────────────────

public static class McpLogLevel
{
    public const string Debug = "debug";
    public const string Info = "info";
    public const string Notice = "notice";
    public const string Warning = "warning";
    public const string Error = "error";
    public const string Critical = "critical";
    public const string Alert = "alert";
    public const string Emergency = "emergency";

    private static readonly string[] Ordered =
    [
        Debug, Info, Notice, Warning, Error, Critical, Alert, Emergency,
    ];

    public static bool IsValid(string? level) =>
        level is not null && Array.IndexOf(Ordered, level) >= 0;

    public static int Rank(string level)
    {
        var index = Array.IndexOf(Ordered, level);
        return index < 0 ? 1 : index;
    }
}

public sealed class LoggingNotification
{
    [JsonPropertyName("level")] public string Level { get; set; } = McpLogLevel.Info;
    [JsonPropertyName("logger")] public string? Logger { get; set; }
    [JsonPropertyName("data")] public object? Data { get; set; }
}

// ── Pagination ────────────────────────────────────────────────────────────

public sealed class CompletionResult
{
    [JsonPropertyName("completion")] public CompletionPayload Completion { get; set; } = new();
}

public sealed class CompletionPayload
{
    [JsonPropertyName("values")] public List<string> Values { get; set; } = [];
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("hasMore")] public bool HasMore { get; set; }
}
