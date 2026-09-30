using HalalChain.Mcp.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HalalChain.Mcp.Tests;

/// <summary>
/// Guards for the expanded tool surface: the original eight plus eight
/// code-introspection tools plus nine governance-introspection tools.
///
/// The exact count matters. A tool that fails to register is invisible to a
/// client that only checks whether *some* tools work, so the registry total
/// and the expected name list are both asserted.
/// </summary>
public class IntrospectionToolsTests
{
    private static readonly string[] IntrospectionTools =
    [
        // Wave 1 — code introspection.
        "halalchain_list_endpoints",
        "halalchain_list_mediator_handlers",
        "halalchain_list_domain_events",
        "halalchain_list_aggregate_roots",
        "halalchain_list_value_objects",
        "halalchain_list_migrations",
        "halalchain_test_inventory",
        "halalchain_find_type",

        // Wave 2 — governance introspection.
        "halalchain_get_principles",
        "halalchain_list_adrs",
        "halalchain_get_adr",
        "halalchain_list_skills",
        "halalchain_get_skill",
        "halalchain_get_controls",
        "halalchain_get_service_manifest",
        "halalchain_verify_no_verdict_authority",
        "halalchain_list_governance_workflows",
    ];

    private static IToolRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();
        return services.BuildServiceProvider().GetRequiredService<IToolRegistry>();
    }

    [Fact]
    public void Registry_Exposes_Exactly_25_Tools() =>
        Assert.Equal(25, BuildRegistry().All.Count);

    [Fact]
    public void Registry_Contains_Every_Introspection_Tool()
    {
        var names = BuildRegistry().All.Select(t => t.Name).ToList();

        foreach (var name in IntrospectionTools)
        {
            Assert.Contains(name, names);
        }
    }

    [Fact]
    public void Every_Introspection_Tool_Is_Read_Only_Local_And_Titled()
    {
        var registry = BuildRegistry();

        foreach (var name in IntrospectionTools)
        {
            Assert.True(registry.TryGet(name, out var tool), name);
            Assert.True(tool.Annotations.ReadOnlyHint, $"{name} must declare readOnlyHint");
            Assert.False(tool.Annotations.DestructiveHint, $"{name} must not declare destructiveHint");
            Assert.True(tool.Annotations.IdempotentHint, $"{name} must declare idempotentHint");
            Assert.False(tool.Annotations.OpenWorldHint, $"{name} reads the local repository, not the network");
            Assert.False(string.IsNullOrWhiteSpace(tool.Annotations.Title), $"{name} needs a human title");
            Assert.NotEmpty(tool.Description);
        }
    }

    [Fact]
    public async Task Every_Introspection_Tool_Executes_Against_The_Repository()
    {
        var registry = BuildRegistry();

        foreach (var name in IntrospectionTools)
        {
            Assert.True(registry.TryGet(name, out var tool), name);
            var markdown = await tool.ExecuteAsync(default);
            Assert.False(string.IsNullOrWhiteSpace(markdown), $"{name} produced no output");
        }
    }

    [Fact]
    public async Task Missing_Required_Arguments_Are_Reported_As_Tool_Errors()
    {
        var registry = BuildRegistry();

        foreach (var name in new[] { "halalchain_get_adr", "halalchain_get_skill", "halalchain_find_type" })
        {
            Assert.True(registry.TryGet(name, out var tool), name);
            var result = await tool.ExecuteDetailedAsync(default);
            Assert.True(result.IsError, $"{name} must report a missing argument as isError");
            Assert.NotEmpty(result.Content);
        }
    }

    [Fact]
    public void Services_Resolve_From_The_Container_And_Scan_The_Repository()
    {
        var services = new ServiceCollection();
        services.AddHalalChainMcp();
        var provider = services.BuildServiceProvider();

        var introspection = provider.GetRequiredService<ICodeIntrospectionService>();
        var governance = provider.GetRequiredService<IGovernanceService>();

        // A positive assertion, not just "does not throw": this type really
        // exists in the scanned tree, so an empty answer means the root
        // resolution broke, not that scanning is legitimately empty.
        var matches = introspection.FindType("HalalChainOptions");
        Assert.NotEmpty(matches);
        Assert.Contains(matches, m => m.RelativePath.EndsWith("HalalChainOptions.cs", StringComparison.Ordinal));

        Assert.NotNull(governance.GetPrinciples());
        Assert.NotNull(governance.ListAdrs());
        Assert.NotNull(governance.ListSkills());
    }
}