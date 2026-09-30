using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-1 tool: EF Core migrations.</summary>
public sealed class ListMigrationsTool : ITool
{
    private readonly ICodeIntrospectionService _introspection;

    public ListMigrationsTool(ICodeIntrospectionService introspection)
    {
        _introspection = introspection;
    }

    public string Name => "halalchain_list_migrations";

    public string Description =>
        "Lists EF Core migrations, ordered by the timestamp embedded in the file name, with their owning project.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional owning project name" }
        },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List EF Core migrations");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var migrations = _introspection.GetMigrations(ToolArguments.String(arguments, "project"));
        var output = $"# EF Core Migrations ({migrations.Count})\n\n";

        if (migrations.Count == 0)
        {
            return Task.FromResult(
                output +
                $"Nothing found. The scan walks the solution root — check `{HalalChainOptions.GetSolutionRootEnvVar()}` " +
                "if this should be non-empty.\n");
        }

        foreach (var group in migrations.GroupBy(m => m.ProjectName).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            output += $"## {group.Key}\n\n";
            foreach (var migration in group)
            {
                output += $"- `{migration.Timestamp}` **{migration.Name}**\n  `{migration.RelativePath}`\n";
            }

            output += "\n";
        }

        output += "Detection: files under a `Migrations/` folder, ordered by the `yyyyMMddHHmmss_` prefix in the file name. " +
                  "Listing migrations reports what exists — applying them is an operations action, not this tool's.\n";
        return Task.FromResult(output);
    }
}