using System.Text.Json;
using HalalChain.Mcp;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HalalChain.Mcp.Tests;

/// <summary>
/// Covers the MCP 2025-06-18 surface added on top of the original
/// text-only tool server: version negotiation, tool annotations, structured
/// tool results, resources, prompts, and argument completion.
/// </summary>
public class ProtocolNegotiationTests
{
    [Theory]
    [InlineData("2025-06-18", "2025-06-18")]
    [InlineData("2025-03-26", "2025-03-26")]
    [InlineData("2024-11-05", "2024-11-05")]
    public void Negotiate_Honours_Supported_Revision(string requested, string expected) =>
        Assert.Equal(expected, McpProtocol.Negotiate(requested));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2099-01-01")]
    [InlineData("not-a-version")]
    public void Negotiate_Falls_Back_To_Latest(string? requested) =>
        Assert.Equal(McpProtocol.Latest, McpProtocol.Negotiate(requested));

    [Fact]
    public void Structured_Content_Requires_2025_06_18()
    {
        Assert.True(McpProtocol.SupportsStructuredContent("2025-06-18"));
        Assert.False(McpProtocol.SupportsStructuredContent("2025-03-26"));
        Assert.False(McpProtocol.SupportsStructuredContent("2024-11-05"));
    }

    [Fact]
    public void Annotations_Require_2025_03_26()
    {
        Assert.True(McpProtocol.SupportsAnnotations("2025-06-18"));
        Assert.True(McpProtocol.SupportsAnnotations("2025-03-26"));
        Assert.False(McpProtocol.SupportsAnnotations("2024-11-05"));
    }
}

public class ToolAnnotationTests
{
    private static IToolRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();
        return services.BuildServiceProvider().GetRequiredService<IToolRegistry>();
    }

    [Fact]
    public void Every_Tool_Is_Annotated_ReadOnly_And_NonDestructive()
    {
        foreach (var tool in BuildRegistry().All)
        {
            Assert.True(tool.Annotations.ReadOnlyHint, $"{tool.Name} must declare readOnlyHint");
            Assert.False(tool.Annotations.DestructiveHint, $"{tool.Name} must not declare destructiveHint");
            Assert.True(tool.Annotations.IdempotentHint, $"{tool.Name} must declare idempotentHint");
        }
    }

    [Fact]
    public void Every_Tool_Carries_A_Human_Title()
    {
        foreach (var tool in BuildRegistry().All)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Annotations.Title), $"{tool.Name} needs a title");
        }
    }

    [Fact]
    public void Only_Network_Tools_Declare_OpenWorld()
    {
        var registry = BuildRegistry();

        Assert.True(registry.TryGet("halalchain_health", out var health));
        Assert.True(health.Annotations.OpenWorldHint, "health probes running services");

        Assert.True(registry.TryGet("halalchain_list_projects", out var projects));
        Assert.False(projects.Annotations.OpenWorldHint, "project listing reads the local repo only");
    }
}

public class StructuredToolResultTests
{
    [Fact]
    public async Task ExecuteDetailedAsync_Wraps_Text_Content_Block()
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();
        var registry = services.BuildServiceProvider().GetRequiredService<IToolRegistry>();

        Assert.True(registry.TryGet("halalchain_get_architecture", out var tool));

        var result = await tool.ExecuteDetailedAsync(default);

        Assert.NotEmpty(result.Content);
        Assert.Equal("text", result.Content[0].Type);
        Assert.False(string.IsNullOrWhiteSpace(result.Content[0].Text));
        Assert.False(result.IsError);
    }

    [Fact]
    public async Task Text_Only_Projection_Is_Unchanged_For_Older_Clients()
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();
        var registry = services.BuildServiceProvider().GetRequiredService<IToolRegistry>();

        Assert.True(registry.TryGet("halalchain_list_projects", out var tool));

        var markdown = await tool.ExecuteAsync(default);
        var detailed = await tool.ExecuteDetailedAsync(default);

        Assert.Equal(markdown, detailed.Content[0].Text);
    }
}

public class ResourceTests
{
    private static IResourceRegistry BuildRegistry(out string root)
    {
        root = FindRepositoryRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HalalChain:SolutionRoot"] = root,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddHalalChainMcp(configuration);
        return services.BuildServiceProvider().GetRequiredService<IResourceRegistry>();
    }

    [Fact]
    public void List_Exposes_Synthetic_And_File_Resources()
    {
        var registry = BuildRegistry(out _);
        var uris = registry.All.Select(r => r.Uri).ToList();

        Assert.Contains("halalchain://architecture", uris);
        Assert.Contains("halalchain://solution/projects", uris);
        Assert.Contains("halalchain://docs/index", uris);
        Assert.Contains("file://AGENTS.md", uris);
    }

    [Fact]
    public void Every_Resource_Declares_Name_And_MimeType()
    {
        var registry = BuildRegistry(out _);

        Assert.All(registry.All, resource =>
        {
            Assert.False(string.IsNullOrWhiteSpace(resource.Name));
            Assert.False(string.IsNullOrWhiteSpace(resource.MimeType));
        });
    }

    [Fact]
    public void Read_Architecture_Synthetic_Resource()
    {
        var registry = BuildRegistry(out _);

        Assert.True(registry.TryRead("halalchain://architecture", out var contents, out var error), error);
        Assert.Equal("text/markdown", contents!.MimeType);
        Assert.Contains("Platform API", contents.Text);
    }

    [Fact]
    public void Read_Project_Inventory_As_Json()
    {
        var registry = BuildRegistry(out _);

        Assert.True(registry.TryRead("halalchain://solution/projects", out var contents, out var error), error);
        Assert.Equal("application/json", contents!.MimeType);

        using var document = JsonDocument.Parse(contents.Text!);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.NotEmpty(document.RootElement.EnumerateArray());
    }

    [Fact]
    public void Read_Documentation_Index_Lists_Runbooks()
    {
        var registry = BuildRegistry(out _);

        Assert.True(registry.TryRead("halalchain://runbooks/index", out var contents, out var error), error);

        using var document = JsonDocument.Parse(contents.Text!);
        var paths = document.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("path").GetString() ?? string.Empty)
            .ToList();

        Assert.Contains(paths, p => p.StartsWith("docs/runbooks/", StringComparison.Ordinal));
    }

    [Fact]
    public void Reads_A_Real_Documentation_File()
    {
        var registry = BuildRegistry(out _);

        Assert.True(
            registry.TryRead("file://docs/ARCHITECTURE.md", out var contents, out var error),
            error);
        Assert.Equal("text/markdown", contents!.MimeType);
        Assert.Contains("HalalChain", contents.Text);
    }

    [Theory]
    [InlineData("file://docs/../../secrets.txt")]
    [InlineData("file://../../Windows/System32/drivers/etc/hosts")]
    [InlineData("file://docs/../../../etc/passwd")]
    public void Rejects_Path_Traversal(string uri)
    {
        var registry = BuildRegistry(out _);

        Assert.False(registry.TryRead(uri, out var contents, out var error));
        Assert.Null(contents);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Rejects_Directories_Outside_The_Allowlist()
    {
        var registry = BuildRegistry(out _);

        // The solution file is at the root but is not on the root allowlist.
        Assert.False(registry.TryRead("file://HalalChain.Platform.sln", out _, out var error));
        Assert.Contains("not served", error);
    }

    [Fact]
    public void Rejects_Unknown_Synthetic_Name()
    {
        var registry = BuildRegistry(out _);

        Assert.False(registry.TryRead("halalchain://nope", out _, out var error));
        Assert.Contains("Unknown resource", error);
    }

    [Fact]
    public void Rejects_Empty_And_Foreign_Schemes()
    {
        var registry = BuildRegistry(out _);

        Assert.False(registry.TryRead("", out _, out _));
        Assert.False(registry.TryRead("https://example.com/evil.md", out _, out var error));
        Assert.Contains("halalchain://", error);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HalalChain.Platform.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "HalalChain.Platform.sln was not found above " + AppContext.BaseDirectory);
    }
}

public class PromptTests
{
    private static IPromptRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();
        return services.BuildServiceProvider().GetRequiredService<IPromptRegistry>();
    }

    [Fact]
    public void List_Exposes_Domain_Prompts()
    {
        var names = BuildRegistry().All.Select(p => p.Name).ToList();

        Assert.Contains("halal-compliance-review", names);
        Assert.Contains("policy-engine-audit", names);
        Assert.Contains("incident-triage", names);
        Assert.Contains("module-onboarding", names);
        Assert.Contains("mcp-capability-audit", names);
    }

    [Fact]
    public void Every_Prompt_Is_Described_And_Namespaced()
    {
        foreach (var prompt in BuildRegistry().All)
        {
            Assert.False(string.IsNullOrWhiteSpace(prompt.Description), $"{prompt.Name} needs a description");
            Assert.All(prompt.Arguments, a => Assert.False(string.IsNullOrWhiteSpace(a.Name)));
        }
    }

    [Fact]
    public void Renders_With_Supplied_Arguments()
    {
        var registry = BuildRegistry();
        var arguments = new Dictionary<string, string>
        {
            ["subject"] = "Satay Sauce 250ml",
            ["jurisdiction"] = "MY",
        };

        Assert.True(
            registry.TryRender("halal-compliance-review", arguments, out var result, out var error),
            error);

        var text = result!.Messages[0].Content.Text!;
        Assert.Contains("Satay Sauce 250ml", text);
        Assert.Contains("MY-v3", text);
        Assert.Equal("user", result.Messages[0].Role);
    }

    [Fact]
    public void Defaults_Jurisdiction_When_Omitted()
    {
        var registry = BuildRegistry();
        var arguments = new Dictionary<string, string> { ["subject"] = "Coconut Milk" };

        Assert.True(registry.TryRender("halal-compliance-review", arguments, out var result, out var error), error);
        Assert.Contains("MY-v3", result!.Messages[0].Content.Text);
    }

    [Fact]
    public void Rejects_Missing_Required_Argument()
    {
        var registry = BuildRegistry();

        Assert.False(registry.TryRender("halal-compliance-review", new Dictionary<string, string>(), out var result, out var error));
        Assert.Null(result);
        Assert.Contains("subject", error);
    }

    [Fact]
    public void Rejects_Unknown_Prompt()
    {
        Assert.False(BuildRegistry().TryRender("no-such-prompt", new Dictionary<string, string>(), out _, out var error));
        Assert.Contains("Unknown prompt", error);
    }

    [Fact]
    public void Prompts_Enforce_The_Determinism_Invariant()
    {
        var registry = BuildRegistry();
        var arguments = new Dictionary<string, string> { ["subject"] = "Palm Oil" };

        Assert.True(registry.TryRender("halal-compliance-review", arguments, out var result, out var error), error);

        var text = result!.Messages[0].Content.Text!;
        Assert.Contains("Deterministic systems decide", text);
        Assert.Contains("Never assign", text);
    }

    [Fact]
    public void Argument_Completion_Offers_Known_Jurisdictions()
    {
        var registry = BuildRegistry();

        var matches = registry.SuggestValues("halal-compliance-review", "jurisdiction", string.Empty);

        Assert.Contains("MY", matches);
        Assert.Contains("ID", matches);
        Assert.Contains("GCC", matches);
    }

    [Fact]
    public void Argument_Completion_Filters_By_Prefix()
    {
        var registry = BuildRegistry();

        var matches = registry.SuggestValues("halal-compliance-review", "jurisdiction", "M");

        Assert.Contains("MY", matches);
        Assert.DoesNotContain("ID", matches);
    }

    [Fact]
    public void Argument_Completion_Is_Empty_For_Unknown_Prompts()
    {
        Assert.Empty(BuildRegistry().SuggestValues("nope", "jurisdiction", string.Empty));
    }
}

public class McpOptionsTests
{
    [Fact]
    public void New_Features_Are_On_By_Default()
    {
        var options = new McpOptions();

        Assert.True(options.EnableResources);
        Assert.True(options.EnablePrompts);
        Assert.True(options.EnableCompletions);
        Assert.True(options.EnableProgress);
        Assert.True(options.EnableLogging);
        Assert.True(options.EnableSampling);
        Assert.True(options.EnableElicitation);
    }

    [Fact]
    public void Resource_And_Timeout_Ceilings_Have_Defaults()
    {
        var options = new McpOptions();

        Assert.Equal(524_288, options.MaxResourceBytes);
        Assert.Equal(60, options.ClientRequestTimeoutSeconds);
        Assert.Equal(50, options.PageSize);
    }

    [Fact]
    public void Log_Level_Defaults_To_Information() =>
        Assert.Equal(LogLevelOption.Information, new McpOptions().LogLevel);
}

public class LogLevelTests
{
    [Theory]
    [InlineData("debug", 0)]
    [InlineData("info", 1)]
    [InlineData("warning", 3)]
    [InlineData("emergency", 7)]
    public void Rank_Orders_Severity(string level, int expected) =>
        Assert.Equal(expected, McpLogLevel.Rank(level));

    [Theory]
    [InlineData("warning", true)]
    [InlineData("debug", true)]
    [InlineData("verbose", false)]
    [InlineData(null, false)]
    public void Validity_Gates_The_Level(string? level, bool valid) =>
        Assert.Equal(valid, McpLogLevel.IsValid(level));
}
