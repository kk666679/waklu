using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;
using HalalChain.Mcp.Services;
using HalalChain.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HalalChain.Mcp;

/// <summary>
/// Stdio MCP host.
///
/// Speaks JSON-RPC 2.0 over stdin/stdout and implements the 2025-06-18
/// revision: tool annotations, structured tool output, resources, prompts,
/// argument completion, the logging channel, progress notifications, cursor
/// pagination, JSON-RPC batching, and the bidirectional primitives
/// (sampling, elicitation) — each gated on both an option and the capability
/// the client declared during the handshake.
/// </summary>
public static class Program
{
    private const string ServerName = "halalchain-mcp";
    private const string ServerVersion = "2.0.0";

    private const int ParseError = -32700;
    private const int InvalidRequest = -32600;
    private const int MethodNotFound = -32601;
    private const int InvalidParams = -32602;
    private const int InternalError = -32603;

    private const string Instructions =
        "HalalChain platform context. AI collects and interprets evidence; the deterministic " +
        "Policy Engine in tawheed assigns every halal verdict. Use the tools to inspect the " +
        "solution and running services, the resources for architecture and documentation, and " +
        "the prompts for compliance review, determinism audit, and incident triage workflows. " +
        "Never infer or override a compliance status yourself.";

    public static async Task Main(string[] args)
    {
        // stdout is the frame transport. Capture the raw stream first, then
        // point Console.Out at stderr so a stray Console.WriteLine anywhere in
        // the process cannot inject non-protocol bytes into the stream.
        var stdout = Console.OpenStandardOutput();
        var stdin = Console.OpenStandardInput();
        var transport = new TransportWriter(new StreamWriter(stdout, new UTF8Encoding(false))
        {
            AutoFlush = false,
            NewLine = "\n",
        });
        Console.SetOut(Console.Error);

        var services = new ServiceCollection();
        services.AddHalalChainMcp(transportWriter: transport);

        // The provider is owned by this scope so the host shuts down
        // deterministically. It is deliberately not cached in a static field:
        // that previously leaked a provider across runs.
        await using var provider = services.BuildServiceProvider();

        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(ServerName);
        var options = provider.GetRequiredService<IOptions<HalalChainOptions>>().Value.Mcp;

        var context = new SessionContext(
            provider.GetRequiredService<IMcpSession>(),
            provider.GetRequiredService<IClientMessenger>(),
            provider.GetRequiredService<IToolRegistry>(),
            provider.GetRequiredService<IResourceRegistry>(),
            provider.GetRequiredService<IPromptRegistry>(),
            options,
            logger);

        var reader = new StreamReader(stdin, new UTF8Encoding(false));

        using var shutdownCts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            logger.LogInformation("Shutdown signal received");
            shutdownCts.Cancel();
        };

        logger.LogInformation(
            "{Server} started; protocol={Protocol}; transport=stdio",
            ServerName,
            McpProtocol.Latest);

        try
        {
            await ReadLoopAsync(reader, transport, context, shutdownCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            logger.LogInformation("{Server} shutting down", ServerName);
        }
    }

    private static async Task ReadLoopAsync(
        StreamReader reader,
        TransportWriter transport,
        SessionContext context,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                context.Logger.LogWarning(ex, "stdin read failed; exiting");
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            if (line is null)
            {
                break;
            }

            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.Length > context.Options.MaxRequestBytes)
            {
                context.Logger.LogWarning(
                    "Request exceeded max size: {Size} bytes", line.Length);
                await transport
                    .WriteFrameAsync(ErrorResponse(null, InvalidRequest, "Request too large"), ct)
                    .ConfigureAwait(false);
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(line);
                await RouteAsync(document.RootElement, transport, context, ct).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                context.Logger.LogWarning(ex, "Invalid JSON received");
                await transport
                    .WriteFrameAsync(ErrorResponse(null, ParseError, "Parse error"), ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                // The pipe is gone; there is nothing left to serve.
                context.Logger.LogWarning(ex, "Transport write failed; exiting");
                break;
            }
            catch (Exception ex)
            {
                // Report and keep serving. Losing the session over one
                // unexpected frame is worse than logging it.
                context.Logger.LogError(ex, "Failed processing frame; continuing");
            }
        }
    }

    /// <summary>
    /// Classifies one inbound frame and either completes a pending
    /// server-initiated request or dispatches the frame as a client request.
    /// Every outcome is written to the transport by this method or its callees.
    /// </summary>
    private static async Task RouteAsync(
        JsonElement root,
        TransportWriter transport,
        SessionContext context,
        CancellationToken ct)
    {
        if (IsResponseFrame(root))
        {
            // A well-formed response with no matching pending request is
            // discarded silently, as JSON-RPC requires.
            context.Messenger.TryCompleteResponse(root);
            return;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            await HandleBatchAsync(root, transport, context, ct).ConfigureAwait(false);
            return;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            await transport
                .WriteFrameAsync(
                    ErrorResponse(null, InvalidRequest, "Expected a JSON-RPC object or batch array"), ct)
                .ConfigureAwait(false);
            return;
        }

        var request = DeserializeRequest(root);
        if (request is null)
        {
            await transport
                .WriteFrameAsync(ErrorResponse(null, InvalidRequest, "Malformed request envelope"), ct)
                .ConfigureAwait(false);
            return;
        }

        var response = await DispatchAsync(request, context, ct).ConfigureAwait(false);
        if (response is not null)
        {
            await transport.WriteFrameAsync(response, ct).ConfigureAwait(false);
        }
    }

    private static async Task HandleBatchAsync(
        JsonElement root,
        TransportWriter transport,
        SessionContext context,
        CancellationToken ct)
    {
        var responses = new List<JsonRpcResponse>();

        foreach (var item in root.EnumerateArray())
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (IsResponseFrame(item))
            {
                context.Messenger.TryCompleteResponse(item);
                continue;
            }

            var request = DeserializeRequest(item);
            if (request is null)
            {
                continue;
            }

            var response = await DispatchAsync(request, context, ct).ConfigureAwait(false);
            if (response is not null)
            {
                responses.Add(response);
            }
        }

        // An all-notification batch produces no output at all.
        if (responses.Count > 0)
        {
            await transport.WriteFrameAsync(responses, ct).ConfigureAwait(false);
        }
    }

    private static async Task<JsonRpcResponse?> DispatchAsync(
        JsonRpcRequest request,
        SessionContext context,
        CancellationToken ct)
    {
        try
        {
            // Notifications carry no id and expect no response.
            if (request.Method.StartsWith("notifications/", StringComparison.Ordinal))
            {
                context.Logger.LogDebug("Received notification {Method}", request.Method);

                if (request.Method == "notifications/cancelled")
                {
                    // Nothing long-running survives past its own timeout, so a
                    // cancellation only needs acknowledging, which a notification
                    // does by definition.
                    context.Logger.LogDebug(
                        "Client cancelled request {RequestId}", ReadString(request.Params, "requestId"));
                }

                return null;
            }

            var result = await HandleMethodAsync(request, context, ct).ConfigureAwait(false);
            return new JsonRpcResponse { Id = request.Id, Result = result };
        }
        catch (RpcException ex)
        {
            context.Logger.LogWarning(
                "Request {Method} rejected ({Code}): {Message}", request.Method, ex.Code, ex.Message);
            return ErrorResponse(request.Id, ex.Code, ex.Message);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return ErrorResponse(request.Id, InternalError, "Operation timed out");
        }
        catch (Exception ex)
        {
            // A single bad request must not take the session down: the client
            // is mid-conversation and expects an error, not a dropped pipe.
            context.Logger.LogError(ex, "Error handling request {Method}", request.Method);
            return ErrorResponse(request.Id, InternalError, "Internal error");
        }
    }

    private static async Task<object?> HandleMethodAsync(
        JsonRpcRequest request,
        SessionContext context,
        CancellationToken ct) => request.Method switch
        {
            "initialize" => HandleInitialize(request, context),
            "ping" => new Dictionary<string, object?>(),
            "tools/list" => HandleToolsList(request, context),
            "tools/call" => await HandleToolsCallAsync(request, context, ct).ConfigureAwait(false),
            "resources/list" => HandleResourcesList(request, context),
            "resources/templates/list" => HandleResourceTemplatesList(context),
            "resources/read" => HandleResourcesRead(request, context),
            "prompts/list" => HandlePromptsList(request, context),
            "prompts/get" => HandlePromptsGet(request, context),
            "completion/complete" => HandleCompletionComplete(request, context),
            "logging/setLevel" => HandleLoggingSetLevel(request, context),
            _ => throw new RpcException(MethodNotFound, $"Unknown method: {request.Method}"),
        };

    // ── Handshake ───────────────────────────────────────────────────────────

    private static object HandleInitialize(JsonRpcRequest request, SessionContext context)
    {
        var requested = ReadString(request.Params, "protocolVersion");
        var negotiated = McpProtocol.Negotiate(requested);

        context.Session.CompleteInitialize(
            negotiated,
            ReadCapabilities(request.Params),
            ReadClientIdentity(request.Params));

        context.Logger.LogInformation(
            "Client {Client} negotiated protocol {Version}",
            context.Session.Client?.Name ?? "(unnamed)",
            negotiated);

        var capabilities = new Dictionary<string, object?>
        {
            ["tools"] = new Dictionary<string, object?> { ["listChanged"] = false },
        };

        if (McpProtocol.SupportsAnnotations(negotiated))
        {
            if (context.Options.EnablePrompts)
            {
                capabilities["prompts"] = new Dictionary<string, object?> { ["listChanged"] = false };
            }

            if (context.Options.EnableResources)
            {
                capabilities["resources"] = new Dictionary<string, object?>
                {
                    ["subscribe"] = false,
                    ["listChanged"] = false,
                };
            }

            if (context.Options.EnableLogging)
            {
                capabilities["logging"] = new Dictionary<string, object?>();
            }

            if (context.Options.EnableCompletions)
            {
                capabilities["completions"] = new Dictionary<string, object?>();
            }
        }

        return new Dictionary<string, object?>
        {
            ["protocolVersion"] = negotiated,
            ["capabilities"] = capabilities,
            ["serverInfo"] = new Dictionary<string, object?>
            {
                ["name"] = ServerName,
                ["title"] = "HalalChain Platform",
                ["version"] = ServerVersion,
            },
            ["instructions"] = Instructions,
        };
    }

    // ── Tools ───────────────────────────────────────────────────────────────

    private static object HandleToolsList(JsonRpcRequest request, SessionContext context)
    {
        var all = context.Tools.All.OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
        var (page, nextCursor) = Paginate(all, ReadString(request.Params, "cursor"), context.Options.PageSize);
        var version = context.Session.ProtocolVersion ?? McpProtocol.Latest;
        var withAnnotations = McpProtocol.SupportsAnnotations(version);

        var tools = page.Select(tool => new Dictionary<string, object?>
        {
            ["name"] = tool.Name,
            ["title"] = tool.Annotations.Title,
            ["description"] = tool.Description,
            ["inputSchema"] = tool.InputSchema,
            ["outputSchema"] = tool.OutputSchema,
            ["annotations"] = withAnnotations ? new Dictionary<string, object?>
            {
                ["title"] = tool.Annotations.Title,
                ["readOnlyHint"] = tool.Annotations.ReadOnlyHint,
                ["destructiveHint"] = tool.Annotations.DestructiveHint,
                ["idempotentHint"] = tool.Annotations.IdempotentHint,
                ["openWorldHint"] = tool.Annotations.OpenWorldHint,
            } : null,
        }).ToArray();

        return new Dictionary<string, object?>
        {
            ["tools"] = tools,
            ["nextCursor"] = nextCursor,
        };
    }

    private static async Task<object> HandleToolsCallAsync(
        JsonRpcRequest request,
        SessionContext context,
        CancellationToken ct)
    {
        if (request.Params is not { ValueKind: JsonValueKind.Object } parameters)
        {
            throw new RpcException(InvalidParams, "Missing params for tools/call");
        }

        var name = ReadString(request.Params, "name");
        if (string.IsNullOrEmpty(name))
        {
            throw new RpcException(InvalidParams, "Missing tool name");
        }

        if (!context.Tools.TryGet(name, out var tool))
        {
            throw new RpcException(MethodNotFound, $"Unknown tool: {name}");
        }

        var arguments = parameters.TryGetProperty("arguments", out var args) ? args : default;
        var progressToken = ReadProgressToken(parameters);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, context.Options.ToolTimeoutSeconds)));

        var started = Stopwatch.GetTimestamp();
        context.Logger.LogInformation("Executing tool {ToolName}", name);
        await context.Messenger
            .ReportProgressAsync(progressToken, 0, 1, $"Running {name}", ct)
            .ConfigureAwait(false);

        CallToolResult result;
        try
        {
            result = await tool.ExecuteDetailedAsync(arguments, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            result = new CallToolResult
            {
                IsError = true,
                Content =
                [
                    ContentItem.FromText(
                        $"Tool '{name}' exceeded the {context.Options.ToolTimeoutSeconds}s budget."),
                ],
            };
        }

        var elapsed = Stopwatch.GetElapsed(started);
        context.Logger.LogInformation(
            "Tool {ToolName} completed in {Duration}ms (error={IsError})",
            name, elapsed.TotalMilliseconds, result.IsError == true);

        await context.Messenger
            .ReportProgressAsync(progressToken, 1, 1, "Done", ct)
            .ConfigureAwait(false);

        return SerializeToolResult(result, context.Session.ProtocolVersion ?? McpProtocol.Latest);
    }

    /// <summary>
    /// Projects a tool result onto the wire. <c>structuredContent</c> is only
    /// emitted for revisions that define it; older clients would reject the
    /// extra field.
    /// </summary>
    private static Dictionary<string, object?> SerializeToolResult(CallToolResult result, string version) =>
        new()
        {
            ["content"] = result.Content,
            ["structuredContent"] = McpProtocol.SupportsStructuredContent(version)
                ? result.StructuredContent
                : null,
            ["isError"] = result.IsError,
        };

    // ── Resources ───────────────────────────────────────────────────────────

    private static object HandleResourcesList(JsonRpcRequest request, SessionContext context)
    {
        if (!context.Options.EnableResources)
        {
            throw new RpcException(MethodNotFound, "Resources are disabled on this server");
        }

        var all = context.Resources.All;
        var (page, nextCursor) = Paginate(all, ReadString(request.Params, "cursor"), context.Options.PageSize);

        return new Dictionary<string, object?>
        {
            ["resources"] = page.ToArray(),
            ["nextCursor"] = nextCursor,
        };
    }

    private static object HandleResourceTemplatesList(SessionContext context)
    {
        if (!context.Options.EnableResources)
        {
            throw new RpcException(MethodNotFound, "Resources are disabled on this server");
        }

        // Every resource this server exposes is fully enumerable, so there are
        // no parameterized templates to advertise.
        return new Dictionary<string, object?>
        {
            ["resourceTemplates"] = Array.Empty<object>(),
        };
    }

    private static object HandleResourcesRead(JsonRpcRequest request, SessionContext context)
    {
        if (!context.Options.EnableResources)
        {
            throw new RpcException(MethodNotFound, "Resources are disabled on this server");
        }

        var uri = ReadString(request.Params, "uri");
        if (string.IsNullOrEmpty(uri))
        {
            throw new RpcException(InvalidParams, "Missing resource uri");
        }

        if (!context.Resources.TryRead(uri, out var contents, out var error))
        {
            throw new RpcException(InvalidParams, error ?? "Resource unavailable");
        }

        return new Dictionary<string, object?>
        {
            ["contents"] = new[] { contents! },
        };
    }

    // ── Prompts ─────────────────────────────────────────────────────────────

    private static object HandlePromptsList(JsonRpcRequest request, SessionContext context)
    {
        if (!context.Options.EnablePrompts)
        {
            throw new RpcException(MethodNotFound, "Prompts are disabled on this server");
        }

        var all = context.Prompts.All;
        var (page, nextCursor) = Paginate(all, ReadString(request.Params, "cursor"), context.Options.PageSize);

        return new Dictionary<string, object?>
        {
            ["prompts"] = page.ToArray(),
            ["nextCursor"] = nextCursor,
        };
    }

    private static object HandlePromptsGet(JsonRpcRequest request, SessionContext context)
    {
        if (!context.Options.EnablePrompts)
        {
            throw new RpcException(MethodNotFound, "Prompts are disabled on this server");
        }

        var name = ReadString(request.Params, "name");
        if (string.IsNullOrEmpty(name))
        {
            throw new RpcException(InvalidParams, "Missing prompt name");
        }

        if (!context.Prompts.TryRender(name, ReadStringArguments(request.Params), out var result, out var error))
        {
            throw new RpcException(InvalidParams, error ?? "Prompt unavailable");
        }

        return result!;
    }

    // ── Completion ──────────────────────────────────────────────────────────

    private static object HandleCompletionComplete(JsonRpcRequest request, SessionContext context)
    {
        if (!context.Options.EnableCompletions)
        {
            throw new RpcException(MethodNotFound, "Completions are disabled on this server");
        }

        if (request.Params is not { ValueKind: JsonValueKind.Object } parameters)
        {
            throw new RpcException(InvalidParams, "Missing params for completion/complete");
        }

        // params: { ref: { type: "ref/prompt", name }, argument: { name, value } }
        var referenceType = ReadNestedString(parameters, "ref", "type");
        if (!string.Equals(referenceType, "ref/prompt", StringComparison.Ordinal))
        {
            throw new RpcException(InvalidParams, $"Unsupported completion ref type: {referenceType}");
        }

        var promptName = ReadNestedString(parameters, "ref", "name");
        var argumentName = ReadNestedString(parameters, "argument", "name");
        var prefix = ReadNestedString(parameters, "argument", "value") ?? string.Empty;

        if (string.IsNullOrEmpty(promptName) || string.IsNullOrEmpty(argumentName))
        {
            throw new RpcException(InvalidParams, "completion/complete requires ref.name and argument.name");
        }

        var matches = context.Prompts.SuggestValues(promptName, argumentName, prefix);

        return new CompletionResult
        {
            Completion = new CompletionPayload
            {
                Values = matches.ToList(),
                Total = matches.Count,
                HasMore = false,
            },
        };
    }

    // ── Logging ─────────────────────────────────────────────────────────────

    private static object HandleLoggingSetLevel(JsonRpcRequest request, SessionContext context)
    {
        if (!context.Options.EnableLogging)
        {
            throw new RpcException(MethodNotFound, "Logging is disabled on this server");
        }

        var level = ReadString(request.Params, "level");
        if (!McpLogLevel.IsValid(level))
        {
            throw new RpcException(InvalidParams, $"Invalid log level: {level}");
        }

        context.Messenger.SetLogLevel(level!);
        context.Logger.LogInformation("Client set log level to {Level}", level);

        return new Dictionary<string, object?>();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ClientCapabilities ReadCapabilities(JsonElement? parameters)
    {
        if (parameters is not { ValueKind: JsonValueKind.Object } root ||
            !root.TryGetProperty("capabilities", out var capabilities) ||
            capabilities.ValueKind != JsonValueKind.Object)
        {
            return new ClientCapabilities();
        }

        bool? Flag(string name) =>
            capabilities.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True
                ? true
                : null;

        return new ClientCapabilities
        {
            Roots = Flag("roots"),
            Sampling = Flag("sampling"),
            Elicitation = Flag("elicitation"),
            Tools = Flag("tools"),
            Resources = Flag("resources"),
            Prompts = Flag("prompts"),
            Logging = Flag("logging"),
            Completions = Flag("completions"),
        };
    }

    private static ClientIdentity? ReadClientIdentity(JsonElement? parameters)
    {
        if (parameters is not { ValueKind: JsonValueKind.Object } root ||
            !root.TryGetProperty("clientInfo", out var info) ||
            info.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new ClientIdentity
        {
            Name = info.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
            Version = info.TryGetProperty("version", out var version) ? version.GetString() : null,
            Title = info.TryGetProperty("title", out var title) ? title.GetString() : null,
        };
    }

    private static string? ReadString(JsonElement? parameters, string name)
    {
        if (parameters is not { ValueKind: JsonValueKind.Object } root ||
            !root.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private static string? ReadNestedString(JsonElement root, string parent, string name)
    {
        if (!root.TryGetProperty(parent, out var container) || container.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return container.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static Dictionary<string, string> ReadStringArguments(JsonElement? parameters)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (parameters is not { ValueKind: JsonValueKind.Object } root ||
            !root.TryGetProperty("arguments", out var arguments) ||
            arguments.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in arguments.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                result[property.Name] = property.Value.GetString() ?? string.Empty;
            }
            else if (property.Value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                result[property.Name] = property.Value.GetRawText();
            }
        }

        return result;
    }

    private static string? ReadProgressToken(JsonElement? parameters)
    {
        if (parameters is not { ValueKind: JsonValueKind.Object } root ||
            !root.TryGetProperty("_meta", out var meta) ||
            meta.ValueKind != JsonValueKind.Object ||
            !meta.TryGetProperty("progressToken", out var token))
        {
            return null;
        }

        return token.ValueKind switch
        {
            JsonValueKind.String => token.GetString(),
            JsonValueKind.Number => token.GetRawText(),
            _ => null,
        };
    }

    private static bool IsResponseFrame(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && !element.TryGetProperty("method", out _)
        && (element.TryGetProperty("result", out _) || element.TryGetProperty("error", out _));

    private static JsonRpcRequest? DeserializeRequest(JsonElement element)
    {
        try
        {
            return element.Deserialize<JsonRpcRequest>(McpJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Cursor pagination over an in-memory list. The cursor is the index of the
    /// next element, which is stable because every list this server paginates
    /// is derived from a fixed file scan within a single session.
    /// </summary>
    private static (List<T> Page, string? NextCursor) Paginate<T>(
        IReadOnlyList<T> source,
        string? cursor,
        int pageSize)
    {
        if (pageSize <= 0)
        {
            pageSize = 50;
        }

        var start = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            if (!int.TryParse(cursor, NumberStyles.Integer, CultureInfo.InvariantCulture, out start) ||
                start < 0)
            {
                throw new RpcException(InvalidParams, "Invalid cursor");
            }
        }

        if (start > source.Count)
        {
            start = source.Count;
        }

        var count = Math.Min(pageSize, source.Count - start);
        var page = source.Skip(start).Take(count).ToList();
        var consumed = start + count;

        return (page, consumed < source.Count ? consumed.ToString(CultureInfo.InvariantCulture) : null);
    }

    private static JsonRpcResponse ErrorResponse(JsonElement? id, int code, string message) => new()
    {
        Id = id,
        Error = new JsonRpcError { Code = code, Message = message },
    };

    // ── Types ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Everything a request handler needs. Resolved once because every
    /// collaborator is a stateless singleton; a per-request scope would only
    /// add allocation.
    /// </summary>
    private sealed class SessionContext
    {
        public SessionContext(
            IMcpSession session,
            IClientMessenger messenger,
            IToolRegistry tools,
            IResourceRegistry resources,
            IPromptRegistry prompts,
            McpOptions options,
            ILogger logger)
        {
            Session = session;
            Messenger = messenger;
            Tools = tools;
            Resources = resources;
            Prompts = prompts;
            Options = options;
            Logger = logger;
        }

        public IMcpSession Session { get; }

        public IClientMessenger Messenger { get; }

        public IToolRegistry Tools { get; }

        public IResourceRegistry Resources { get; }

        public IPromptRegistry Prompts { get; }

        public McpOptions Options { get; }

        public ILogger Logger { get; }
    }

    private sealed class RpcException : Exception
    {
        public RpcException(int code, string message) : base(message) => Code = code;

        public int Code { get; }
    }
}
