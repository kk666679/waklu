using System.Text.Json;
using System.Text.Json.Serialization;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HalalChain.Mcp;

public static class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();

        // The MCP server is a short-lived console process. The service
        // collection and provider are owned by `using` blocks so the host
        // shuts down deterministically on exit. We do NOT cache them in
        // static fields — that previously leaked a provider across runs
        // and made shutdown non-deterministic.
        await using var provider = services.BuildServiceProvider();
        using var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("McpServer");

        var mcpOptions = provider.GetRequiredService<IOptions<HalalChainOptions>>().Value.Mcp;

        var stdin = Console.InputEncoding = System.Text.Encoding.UTF8;
        var reader = new StreamReader(Console.OpenStandardInput(), stdin);
        var stdout = Console.OpenStandardOutput();
        var writer = new StreamWriter(stdout) { AutoFlush = true };

        logger.LogInformation("halalchain-mcp started; protocol=2024-11-05");

        using var shutdownCts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            logger.LogInformation("Shutdown signal received");
            shutdownCts.Cancel();
        };

        try
        {
            while (!shutdownCts.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync(shutdownCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "stdin read failed; exiting");
                    break;
                }
                if (line == null) break;

                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.Length > mcpOptions.MaxRequestBytes)
                {
                    logger.LogWarning("Request exceeded max size: {Size} bytes", line.Length);
                    var error = CreateErrorResponse(null, -32600, "Request too large");
                    await writer.WriteLineAsync(JsonSerializer.Serialize(error, JsonOpts));
                    continue;
                }

                JsonRpcResponse? response = null;
                try
                {
                    var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, JsonOpts);
                    if (request == null) continue;

                    response = await HandleRequestAsync(request, provider, logger);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Invalid JSON received");
                    response = CreateErrorResponse(null, -32700, "Parse error");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception processing request");
                    response = CreateErrorResponse(null, -32603, "Internal error");
                }

                if (response != null)
                {
                    var json = JsonSerializer.Serialize(response, JsonOpts);
                    await writer.WriteLineAsync(json);
                }
            }
        }
        finally
        {
            logger.LogInformation("halalchain-mcp shutting down");
            await writer.FlushAsync();
        }
    }

    private static async Task<JsonRpcResponse?> HandleRequestAsync(
        JsonRpcRequest request, IServiceProvider rootProvider, ILogger rootLogger)
    {
        // Each request runs in a fresh scope so per-request state (loggers,
        // tool instances) does not leak across requests. The root provider
        // owns the host lifetime.
        using var scope = rootProvider.CreateScope();
        var toolRegistry = scope.ServiceProvider.GetRequiredService<IToolRegistry>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();

        if (request.Method == "notifications/initialized")
        {
            logger.LogDebug("Received initialized notification (no response expected)");
            return null;
        }

        object? result = null;
        try
        {
            result = request.Method switch
            {
                "initialize" => HandleInitialize(request, rootLogger),
                "tools/list" => HandleToolsList(request, toolRegistry),
                "tools/call" => await HandleToolsCallAsync(request, toolRegistry, logger),
                _ => throw new NotSupportedException($"Unknown method: {request.Method}")
            };
        }
        catch (NotSupportedException ex)
        {
            logger.LogWarning("Unsupported method: {Method}", request.Method);
            return CreateErrorResponse(request.Id, -32601, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Invalid operation: {Message}", ex.Message);
            return CreateErrorResponse(request.Id, -32602, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling request {Method}", request.Method);
            return CreateErrorResponse(request.Id, -32603, "Internal error");
        }

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    private static object HandleInitialize(JsonRpcRequest request, ILogger logger)
    {
        logger.LogInformation("MCP client initializing");
        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { tools = new { listChanged = false } },
            serverInfo = new { name = "halalchain-mcp", version = "1.0.0" }
        };
    }

    private static object HandleToolsList(JsonRpcRequest request, IToolRegistry registry)
    {
        return new
        {
            tools = registry.All.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = t.InputSchema
            }).ToArray()
        };
    }

    private static async Task<object?> HandleToolsCallAsync(JsonRpcRequest request, IToolRegistry registry, ILogger logger)
    {
        var p = request.Params;
        if (p == null || p.Value.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException("Missing params for tools/call");

        var toolName = p.Value.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
        var arguments = p.Value.TryGetProperty("arguments", out var argsEl) ? argsEl : default;

        if (string.IsNullOrEmpty(toolName))
            throw new InvalidOperationException("Missing tool name");

        if (!registry.TryGet(toolName, out var tool) || tool == null)
            throw new NotSupportedException($"Unknown tool: {toolName}");

        logger.LogInformation("Executing tool {ToolName}", toolName);
        var start = DateTime.UtcNow;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await tool.ExecuteAsync(arguments, cts.Token);

        var duration = DateTime.UtcNow - start;
        logger.LogInformation("Tool {ToolName} completed in {Duration}ms", toolName, duration.TotalMilliseconds);

        return new
        {
            content = new[]
            {
                new { type = "text", text = result }
            }
        };
    }

    private static JsonRpcResponse CreateErrorResponse(JsonElement? id, int code, string message)
    {
        return new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError { Code = code, Message = message }
        };
    }
}
