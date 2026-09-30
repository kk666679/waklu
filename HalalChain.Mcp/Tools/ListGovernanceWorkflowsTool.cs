using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-2 tool: governance workflows and state machines.</summary>
public sealed class ListGovernanceWorkflowsTool : ITool
{
    private readonly IGovernanceService _governance;

    public ListGovernanceWorkflowsTool(IGovernanceService governance)
    {
        _governance = governance;
    }

    public string Name => "halalchain_list_governance_workflows";

    public string Description =>
        "Lists governance workflow definitions: tawheed's workflow files and the domain-side state machines that carry status transitions.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional owning project name (filters the state machines)" }
        },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List governance workflows");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var workflows = _governance.GetGovernanceWorkflows(ToolArguments.String(arguments, "project"));
        var output = $"# Governance Workflows ({workflows.Count})\n\n";

        if (workflows.Count == 0)
        {
            return Task.FromResult(
                output +
                $"Nothing found. Expected files under `.halalchain/tawheed/` and `*StateMachine*.cs` — " +
                $"check `{HalalChainOptions.GetSolutionRootEnvVar()}` if this should be non-empty.\n");
        }

        foreach (var group in workflows.GroupBy(w => w.Source).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            output += $"## {group.Key}\n\n";
            foreach (var workflow in group)
            {
                output += $"- **{workflow.Kind}**\n  `{workflow.RelativePath}`\n";
            }

            output += "\n";
        }

        output += "These are *definitions* — what states and transitions exist. Running one is a code-level action, " +
                  "and the compliance verdict itself is produced only by the deterministic Policy Engine.\n";
        return Task.FromResult(output);
    }
}