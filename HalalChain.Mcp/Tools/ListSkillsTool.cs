using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-2 tool: published skills across both conventions.</summary>
public sealed class ListSkillsTool : ITool
{
    private readonly IGovernanceService _governance;

    public ListSkillsTool(IGovernanceService governance)
    {
        _governance = governance;
    }

    public string Name => "halalchain_list_skills";

    public string Description =>
        "Lists published skills from both supported conventions: skills/<slug>/manifest.yaml packages and .clinerules/<slug>.md steering documents.";

    public object InputSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List published skills");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var index = _governance.ListSkills();
        var output = $"# Published Skills ({index.Skills.Count})\n\n";

        if (index.Skills.Count == 0)
        {
            output += "No skills found. Searched:\n\n";
            foreach (var candidate in index.Searched)
            {
                output += $"- `{candidate}`\n";
            }

            return Task.FromResult(output);
        }

        foreach (var group in index.Skills.GroupBy(s => s.Source).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            output += group.Key == "skills-manifest"
                ? "## `skills/<slug>/manifest.yaml` packages\n\n"
                : "## `.clinerules/<slug>.md` steering documents\n\n";

            foreach (var skill in group)
            {
                output += $"- **{skill.Slug}** — {skill.Title}";
                if (skill.Description.Length > 0)
                {
                    output += $"  \n  {skill.Description}";
                }

                output += $"\n  `{skill.RelativePath}`\n";
            }

            output += "\n";
        }

        output += "Skills describe how an agent works a task. A skill never gets to decide a compliance status — " +
                  "it feeds evidence to the Policy Engine like every other caller.\n";
        return Task.FromResult(output);
    }
}