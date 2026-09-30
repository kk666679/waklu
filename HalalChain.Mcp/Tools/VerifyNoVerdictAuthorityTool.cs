using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>
/// Read-only Wave-2 tool: the verdict-authority guard. The guard checks that
/// nothing in the source declares a name that would hand the compliance
/// decision to something non-deterministic. The tool itself only *reads* the
/// report — it can neither grant authority nor exercise it.
/// </summary>
public sealed class VerifyNoVerdictAuthorityTool : ITool
{
    private readonly IGovernanceService _governance;

    public VerifyNoVerdictAuthorityTool(IGovernanceService governance)
    {
        _governance = governance;
    }

    public string Name => "halalchain_verify_no_verdict_authority";

    public string Description =>
        "Scans the C# sources for identifiers that would grant compliance-verdict authority to something other than the deterministic Policy Engine, and reports every match as a candidate for human review.";

    public object InputSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("Verify verdict authority stays with the Policy Engine");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var report = _governance.VerifyNoVerdictAuthority();
        var output = "# Verdict Authority Scan\n\n";

        output += $"**Result: {(report.Clean ? "no forbidden identifiers found" : $"{report.Violations.Count} candidate(s) for review")}**\n\n";
        output += $"- Files scanned: {report.ScannedFiles}\n";
        output += $"- Guard files skipped by design: {report.SkippedGuardFiles}\n";
        output += $"- Forbidden identifier patterns: {report.Patterns.Count}\n\n";

        output += "Patterns:\n\n";
        foreach (var pattern in report.Patterns)
        {
            output += $"- `{pattern}`\n";
        }

        output += "\n";

        if (report.Violations.Count > 0)
        {
            output += "## Candidates\n\n";
            foreach (var violation in report.Violations)
            {
                output += $"- `{violation.Pattern}` — `{violation.RelativePath}:{violation.Line}`\n";
            }

            output += "\nEach candidate is a raw text match, not a confirmed defect: a name is not a capability. " +
                      "A human decides whether the match is an actual authority path — for example, a catalog operation " +
                      "may legitimately mention a status word without deciding anything.\n\n";
        }

        if (report.ScannedFiles == 0)
        {
            output += "**Zero files were scanned** — a scan of nothing must never be read as a clean result.\n\n";
        }

        output += "The guard marker `halalchain:verdict-authority-guard` exempts the files that must name what they forbid " +
                  "(this guard's own declaration and its architecture test); the skip count above is published, not hidden.\n\n" +
                  "This tool reports; it holds no authority. Only the deterministic Policy Engine in tawheed decides a compliance status.\n";
        return Task.FromResult(output);
    }
}