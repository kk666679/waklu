using System.Text.Json;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Abstractions;

/// <summary>
/// Outbound half of the stdio transport: server-initiated requests
/// (<c>sampling/createMessage</c>, <c>elicitation/create</c>) and
/// server-initiated notifications (<c>notifications/progress</c>,
/// <c>notifications/message</c>).
/// </summary>
/// <remarks>
/// Implementations must serialise every write. A stdio MCP transport is a
/// single byte stream, so two frames interleaving mid-line corrupts the
/// session.
/// </remarks>
public interface IClientMessenger
{
    /// <summary>True when the client declared <c>sampling</c> support.</summary>
    bool ClientSupportsSampling { get; }

    /// <summary>True when the client declared <c>elicitation</c> support.</summary>
    bool ClientSupportsElicitation { get; }

    /// <summary>True when the client declared <c>logging</c> support.</summary>
    bool ClientSupportsLogging { get; }

    /// <summary>Lowest severity that will be forwarded as <c>notifications/message</c>.</summary>
    string LogLevel { get; }

    /// <summary>Sets the minimum severity forwarded to the client's log channel.</summary>
    void SetLogLevel(string level);

    /// <summary>
    /// Routes an inbound frame to the pending server-initiated request it
    /// answers. Returns true when the frame was consumed as a response.
    /// </summary>
    bool TryCompleteResponse(JsonElement envelope);

    /// <summary>Sends a notification. Never waits for a reply.</summary>
    Task NotifyAsync(string method, object? parameters, CancellationToken ct = default);

    /// <summary>Sends a request and awaits the matching response envelope.</summary>
    Task<JsonElement?> RequestAsync(string method, object? parameters, CancellationToken ct = default);

    /// <summary>
    /// Emits <c>notifications/progress</c> when the request carried a
    /// <c>_meta.progressToken</c> and the client declared progress support.
    /// A no-op otherwise, so tools can report progress unconditionally.
    /// </summary>
    Task ReportProgressAsync(string? progressToken, double progress, double? total, string? message, CancellationToken ct = default);

    /// <summary>Emits <c>notifications/message</c> when the client accepts logs.</summary>
    Task LogAsync(string level, string message, string? logger, CancellationToken ct = default);

    /// <summary>Asks the client's model to sample. Returns null when unsupported.</summary>
    Task<CreateMessageResult?> RequestSamplingAsync(CreateMessageRequest request, CancellationToken ct = default);

    /// <summary>Asks the client to collect user input. Returns null when unsupported.</summary>
    Task<ElicitResult?> RequestElicitationAsync(string message, object requestedSchema, CancellationToken ct = default);
}
