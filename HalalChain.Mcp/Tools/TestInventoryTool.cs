using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-1 tool: test inventory.</summary>
public sealed class TestInventoryTool : ITool
{
    private readonly ICodeIntrospectionService _introspection;

    public TestInventoryTool(ICodeIntrospectionService introspection)
    {
        _introspection = introspection;
    }

    public string Name => "halalchain_test_inventory";

    public string Description =>
        "Summarises the test inventory per test project: [Fact] and [Theory] counts, inline-data cases, and files.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional project name to restrict the inventory" }
        },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("Inventory tests");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var inventory = _introspection.GetTestInventory(ToolArguments.String(arguments, "project"));
        var output = $"# Test Inventory ({inventory.Count} test projects)\n\n";

        if (inventory.Count == 0)
        {
            return Task.FromResult(
                output +
                $"Nothing found. The scan walks the solution root — check `{HalalChainOptions.GetSolutionRootEnvVar()}` " +
                "if this should be non-empty.\n");
        }

        output += "| Project | Facts | Theories | Test methods | InlineData cases | Files |\n" +
                  "|---|---:|---:|---:|---:|---:|\n";
        foreach (var project in inventory)
        {
            output += $"| {project.ProjectName} | {project.Facts} | {project.Theories} | " +
                      $"{project.TestMethods} | {project.InlineDataCases} | {project.Files} |\n";
        }

        var totalMethods = inventory.Sum(p => p.TestMethods);
        output += $"\n**{totalMethods} test methods** across {inventory.Count} projects.\n\n" +
                  "Detection: `[Fact]` and `[Theory]` attributes in projects whose name contains `Test`. " +
                  "A theory counts as one method, not one case; `InlineData` rows are reported separately.\n";
        return Task.FromResult(output);
    }
}