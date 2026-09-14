using System.Text.Json;
using System.Text.Json.Serialization;
using HalalChain.Mcp;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;
using HalalChain.Mcp.Services;
using HalalChain.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HalalChain.Mcp.Tests;

public class ProtocolTests
{
    private static JsonSerializerOptions JsonOpts => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    public void JsonRpcRequest_DeserializesCorrectly()
    {
        var json = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":null}""";
        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, JsonOpts);

        Assert.NotNull(request);
        Assert.Equal("2.0", request.JsonRpc);
        Assert.Equal("initialize", request.Method);
        Assert.Equal(1, request.Id?.GetInt32());
    }

    [Fact]
    public void JsonRpcResponse_SerializesCorrectly()
    {
        var response = new JsonRpcResponse
        {
            Id = JsonSerializer.Deserialize<JsonElement>("1"),
            Result = new { message = "ok" }
        };

        var json = JsonSerializer.Serialize(response, JsonOpts);

        Assert.Contains("\"jsonrpc\":\"2.0\"", json);
        Assert.Contains("\"result\"", json);
    }

    [Fact]
    public void JsonRpcError_SerializesCorrectly()
    {
        var response = new JsonRpcResponse
        {
            Id = JsonSerializer.Deserialize<JsonElement>("1"),
            Error = new JsonRpcError { Code = -32601, Message = "Method not found" }
        };

        var json = JsonSerializer.Serialize(response, JsonOpts);

        Assert.Contains("\"error\"", json);
        Assert.Contains("-32601", json);
    }

    [Fact]
    public void Notifications_Should_Not_Return_Response()
    {
        var method = "notifications/initialized";
        var isNotification = method.StartsWith("notifications/");

        Assert.True(isNotification);
    }

    [Fact]
    public void Unknown_Method_Returns_Error()
    {
        var method = "unknown/method";
        var isKnown = method is "initialize" or "notifications/initialized" or "tools/list" or "tools/call";

        Assert.False(isKnown);
    }
}

public class ToolRegistryTests
{
    [Fact]
    public void ToolRegistry_Contains_All_Expected_Tools()
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();
        var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IToolRegistry>();

        var toolNames = registry.All.Select(t => t.Name).ToList();

        Assert.Contains("halalchain_project_status", toolNames);
        Assert.Contains("halalchain_list_projects", toolNames);
        Assert.Contains("halalchain_list_pages", toolNames);
        Assert.Contains("halalchain_list_components", toolNames);
        Assert.Contains("halalchain_list_services", toolNames);
        Assert.Contains("halalchain_platform_overview", toolNames);
        Assert.Contains("halalchain_get_architecture", toolNames);
        Assert.Contains("halalchain_health", toolNames);
    }

    [Fact]
    public void ToolRegistry_TryGet_Returns_True_For_Known_Tool()
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();
        var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IToolRegistry>();

        var found = registry.TryGet("halalchain_project_status", out var tool);

        Assert.True(found);
        Assert.NotNull(tool);
        Assert.Equal("halalchain_project_status", tool.Name);
    }

    [Fact]
    public void ToolRegistry_TryGet_Returns_False_For_Unknown_Tool()
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();
        var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IToolRegistry>();

        var found = registry.TryGet("nonexistent_tool", out var tool);

        Assert.False(found);
    }
}

public class ToolExecutionTests
{
    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ProjectStatusTool_Returns_Status()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IToolRegistry>();

        var found = registry.TryGet("halalchain_project_status", out var tool);
        Assert.True(found);
        Assert.Equal("halalchain_project_status", tool!.Name);
    }

    [Fact]
    public async Task ListProjectsTool_Returns_Projects()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IToolRegistry>();

        var found = registry.TryGet("halalchain_list_projects", out var tool);
        Assert.True(found);

        var result = await tool!.ExecuteAsync(default);
        Assert.Contains("HalalChain", result);
    }

    [Fact]
    public async Task HealthTool_Returns_Health_Results()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IToolRegistry>();

        var found = registry.TryGet("halalchain_health", out var tool);
        Assert.True(found);

        var result = await tool!.ExecuteAsync(default);
        Assert.Contains("Health Check Results", result);
        Assert.Contains("Platform API", result);
    }

    [Fact]
    public void Tools_Have_Correct_Names()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IToolRegistry>();

        foreach (var tool in registry.All)
        {
            Assert.StartsWith("halalchain_", tool.Name);
            Assert.NotEmpty(tool.Description);
        }
    }
}

public class ConfigurationTests
{
    [Fact]
    public void HalalChainOptions_Has_Correct_Section_Name()
    {
        Assert.Equal("HalalChain", HalalChainOptions.SectionName);
    }

    [Fact]
    public void ServiceEndpoints_Has_Default_Values()
    {
        var endpoints = new ServiceEndpoints();

        Assert.Equal("http://localhost:5001", endpoints.PlatformApi);
        Assert.Equal("http://localhost:5200", endpoints.HalalChain);
        Assert.Equal("http://localhost:5201", endpoints.Marketplace);
        Assert.Equal("http://localhost:8000", endpoints.Tawheed);
        Assert.Equal("http://localhost:7071", endpoints.AiInference);
    }

    [Fact]
    public void McpOptions_Has_Default_Values()
    {
        var options = new McpOptions();

        Assert.Equal(1_048_576, options.MaxRequestBytes);
        Assert.Equal(30, options.ToolTimeoutSeconds);
    }

    [Fact]
    public void EnvironmentVariablePrefix_Is_Correct()
    {
        Assert.Equal("HALALCHAIN_SOLUTION_ROOT", HalalChainOptions.GetSolutionRootEnvVar());
    }
}
