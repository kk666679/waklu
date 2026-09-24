using System.Text.RegularExpressions;

namespace HalalChain.Platform.Tests.Observability;

public class AlertCoverageTests
{
    private static string FindRepoFile(string relativePath)
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "HalalChain.Platform.sln")))
            dir = dir.Parent;
        return System.IO.Path.Combine(dir!.FullName, relativePath);
    }

    [Fact]
    public void Every_Alert_Has_A_Runbook()
    {
        var rulesDir = FindRepoFile("infrastructure/prometheus/rules");
        var runbooksDir = FindRepoFile("docs/runbooks");

        var missing = new List<string>();
        foreach (var file in System.IO.Directory.EnumerateFiles(rulesDir, "*.yml"))
        {
            var yaml = System.IO.File.ReadAllText(file);
            var matches = Regex.Matches(yaml, @"runbook:\s*(docs/runbooks/[\w\-]+\.md)");
            foreach (Match m in matches)
            {
                var path = System.IO.Path.Combine(runbooksDir, System.IO.Path.GetFileName(m.Groups[1].Value));
                if (!System.IO.File.Exists(path)) missing.Add(m.Groups[1].Value);
            }
        }

        Assert.True(missing.Count == 0,
            "Alerts reference missing runbooks:\n" + string.Join("\n", missing.Distinct()));
    }

    [Fact]
    public void Every_Runbook_Has_Required_Sections()
    {
        var runbooksDir = FindRepoFile("docs/runbooks");
        var requiredSections = new[] { "## Symptoms", "## Triage", "## Mitigation" };
        var violations = new List<string>();

        foreach (var file in System.IO.Directory.EnumerateFiles(runbooksDir, "*.md"))
        {
            var content = System.IO.File.ReadAllText(file);
            foreach (var section in requiredSections)
                if (!content.Contains(section, StringComparison.OrdinalIgnoreCase))
                    violations.Add($"{System.IO.Path.GetFileName(file)}: missing '{section}'");
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void Every_SLO_Has_Recording_Rule_And_Dashboard_Panel()
    {
        var sloYaml = System.IO.File.ReadAllText(FindRepoFile("docs/slo.yaml"));
        var recordingRules = System.IO.File.ReadAllText(FindRepoFile("infrastructure/prometheus/recording-rules.yml"));
        var dashboardsDir = FindRepoFile("infrastructure/grafana/dashboards");
        var dashboards = string.Join(
            "\n",
            System.IO.Directory.EnumerateFiles(dashboardsDir, "*.json")
                .Select(System.IO.File.ReadAllText));

        var sloNames = Regex.Matches(sloYaml, @"^\s*-\s*name:\s*([\w\-]+)", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value).ToList();

        foreach (var slo in sloNames)
        {
            var service = slo.Split('-')[0];
            Assert.Contains(service, recordingRules,
                StringComparison.OrdinalIgnoreCase);
            Assert.Contains(service, dashboards,
                StringComparison.OrdinalIgnoreCase);

            var concern = slo.Contains("latency", StringComparison.OrdinalIgnoreCase)
                ? "latency"
                : slo.Contains("determinism", StringComparison.OrdinalIgnoreCase)
                    ? "determinism"
                    : "error";
            Assert.Contains(concern, dashboards,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}