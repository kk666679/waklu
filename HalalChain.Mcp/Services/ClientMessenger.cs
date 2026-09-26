using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;
using Microsoft.Extensions.Options;

namespace HalalChain.Mcp.Services;

/// <summary>
/// Server-initiated half of the stdio transport.
///
/// Every frame the server writes goes through <see cref="WriteAsync"/>, which
/// holds a gate for the whole write. Without it a notification emitted from a
/// tool while a response is being flushed can interleave mid-line and corrupt
/// the stream, because stdout is a single shared byte channel.
/// </summary>
internal sealed class ClientMessenger : IClientMessenger
{
    private readonly TransportWriter _transport;
    private readonly IMcpSession _session;
    private readonly McpOptions _options;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement?>> _pending =
        new(StringComparer.Ordinal);

    private int _nextRequestId;
    private volatile string _logLevel = McpLogLevel.Info;

    public ClientMessenger(TransportWriter transport, IMcpSession session, IOptions<HalalChainOptions> options)
    {
        _transport = transport;
        _session = session;
        _options = options.Value.Mcp;
    }

    public bool ClientSupportsSampling => _session.Capabilities.Sampling == true;

    public bool ClientSupportsElicitation => _session.Capabilities.Elicitation == true;

    public bool ClientSupportsLogging => _session.Capabilities.Logging == true;

    public string LogLevel => _logLevel;

    public void SetLogLevel(string level)
    {
        if (McpLogLevel.IsValid(level))
        {
            _logLevel = level;
        }
    }

    public bool TryCompleteResponse(JsonElement envelope)
    {
        if (envelope.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!envelope.TryGetProperty("id", out var idElement))
        {
            return false;
        }

        // A frame that also carries "method" is a request, not a response.
        if (envelope.TryGetProperty("method", out _))
        {
            return false;
        }

        var id = ReadId(idElement);
        if (id is null || !_pending.TryGetValue(id, out var completion))
        {
            return false;
        }

        JsonElement? payload = null;

        if (envelope.TryGetProperty("result", out var result) && result.ValueKind != JsonValueKind.Null)
        {
            // Clone: the caller's JsonDocument is disposed once the read loop
            // moves on, and the payload outlives it.
            payload = result.Clone();
        }
        else if (envelope.TryGetProperty("error", out var error))
        {
            var message = error.ValueKind == JsonValueKind.Object &&
                          error.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString()
                : null;

            payload = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(
                    new { error = message ?? "client reported an error" },
                    McpJson.Options));
        }

        completion.TrySetResult(payload);
        return true;
    }

    public Task NotifyAsync(string method, object? parameters, CancellationToken ct = default) =>
        WriteAsync(new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = parameters,
        }, ct);

    public async Task<JsonElement?> RequestAsync(string method, object? parameters, CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _nextRequestId)
            .ToString(CultureInfo.InvariantCulture);

        var completion = new TaskCompletionSource<JsonElement?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _pending[id] = completion;

        try
        {
            await WriteAsync(new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method,
                ["params"] = parameters,
            }, ct).ConfigureAwait(false);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.ClientRequestTimeoutSeconds)));

            using var registration = timeout.Token.Register(() => completion.TrySetResult(null));

            return await completion.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            completion.TrySetResult(null);
            return null;
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    public async Task ReportProgressAsync(
        string? progressToken,
        double progress,
        double? total,
        string? message,
        CancellationToken ct = default)
    {
        // A tool may always report progress; the messenger decides whether the
        // client asked to hear about it.
        if (string.IsNullOrEmpty(progressToken) || !_options.EnableProgress)
        {
            return;
        }

        try
        {
            await NotifyAsync("notifications/progress", new ProgressNotification
            {
                ProgressToken = progressToken,
                Progress = progress,
                Total = total,
                Message = message,
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Progress is advisory; never fail a tool because the client
            // stopped listening.
        }
        catch (IOException)
        {
        }
    }

    public async Task LogAsync(
        string level,
        string message,
        string? logger,
        CancellationToken ct = default)
    {
        if (!_options.EnableLogging || !ClientSupportsLogging)
        {
            return;
        }

        if (McpLogLevel.Rank(level) < McpLogLevel.Rank(_logLevel))
        {
            return;
        }

        try
        {
            await NotifyAsync("notifications/message", new LoggingNotification
            {
                Level = level,
                Logger = logger,
                Data = message,
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
    }

    public async Task<CreateMessageResult?> RequestSamplingAsync(
        CreateMessageRequest request,
        CancellationToken ct = default)
    {
        if (!_options.EnableSampling || !ClientSupportsSampling)
        {
            return null;
        }

        var payload = await RequestAsync("sampling/createMessage", request, ct).ConfigureAwait(false);
        if (payload is null || payload.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            return payload.Value.Deserialize<CreateMessageResult>(McpJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<ElicitResult?> RequestElicitationAsync(
        string message,
        object requestedSchema,
        CancellationToken ct = default)
    {
        if (!_options.EnableElicitation || !ClientSupportsElicitation)
        {
            return null;
        }

        var payload = await RequestAsync("elicitation/create", new Dictionary<string, object?>
        {
            ["message"] = message,
            ["requestedSchema"] = requestedSchema,
        }, ct).ConfigureAwait(false);

        if (payload is null || payload.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            return payload.Value.Deserialize<ElicitResult>(McpJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private Task WriteAsync(object frame, CancellationToken ct) =>
        _transport.WriteFrameAsync(System.Text.Json.JsonSerializer.Serialize(frame, McpJson.Options), ct);

    private static string? ReadId(JsonElement idElement) => idElement.ValueKind switch
    {
        JsonValueKind.String => idElement.GetString(),
        JsonValueKind.Number => idElement.GetRawText(),
        _ => null,
    };
}
