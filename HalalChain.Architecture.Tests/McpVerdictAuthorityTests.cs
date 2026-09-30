// halalchain:verdict-authority-guard
//
// This test must name every identifier it forbids — it is a guard file by
// definition, exactly like HalalChain.Mcp/Services/GovernanceService.cs.
// The guard marker exempts it from the runtime verdict-authority scan; without
// it, the scan would flag the very test that enforces the rule.

using Xunit;

namespace HalalChain.Architecture.Tests;

/// <summary>
/// Source-level guardrails for the MCP server: the tool surface may read
/// anything, but it may never declare a name that would hand a compliance
/// verdict to something other than the deterministic Policy Engine.
/// </summary>
public sealed class McpVerdictAuthorityTests
{
    private const string GuardMarker = "halalchain:verdict-authority-guard";
    private const string McpDirectory = "HalalChain.Mcp";

    /// <summary>
    /// Mirrors <c>ForbiddenPatterns</c> in GovernanceService. The baseline is
    /// stated independently on purpose: if the production list ever shrinks,
    /// <see cref="Runtime_Pattern_List_Covers_The_Baseline"/> fails rather than
    /// the guard silently reporting clean. The production list remains the
    /// source of truth for the runtime scan; this list is what the scan is
    /// *required* to keep.
    /// </summary>
    private static readonly string[] ForbiddenIdentifiers =
    [
        "SetVerdict",
        "WriteVerdict",
        "OverrideTawheed",
        "BypassTawheed",
        "ApproveCertificate",
        "RejectCertificate",
        "DecideCompliance",
        "ForceHalal",
        "MarkHalal",
        "SetHalalStatus",
    ];

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

    private static IEnumerable<string> McpSourceFiles()
    {
        var root = Path.Combine(FindRepositoryRoot(), McpDirectory);

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            // Build output mirrors the sources it was compiled from; scanning
            // it would duplicate every hit and tie the test to build history.
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return file;
        }
    }

    /// <summary>
    /// Every source file in the repository that carries the guard marker — the
    /// same population the runtime verdict-authority scan skips. Walked
    /// repo-wide because guard files legitimately live outside HalalChain.Mcp
    /// (this test file is one), and pruned of build output the way the runtime
    /// scan prunes it.
    /// </summary>
    private static IEnumerable<string> MarkedSourceFiles()
    {
        var root = FindRepositoryRoot();
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", options))
        {
            var relative = Path.GetRelativePath(root, file);
            var segments = relative.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);

            if (segments.Any(s =>
                    s.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals(".kilo", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (File.ReadAllText(file).Contains(GuardMarker, StringComparison.Ordinal))
            {
                yield return file;
            }
        }
    }

    [Fact]
    public void Mcp_Sources_Declare_No_Forbidden_Verdict_Authority_Identifier()
    {
        var offenders = new List<string>();

        foreach (var file in McpSourceFiles())
        {
            var text = File.ReadAllText(file);

            // Guard files name what they forbid; the runtime scan skips them
            // and so does this one.
            if (text.Contains(GuardMarker, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var identifier in ForbiddenIdentifiers)
            {
                if (text.Contains(identifier, StringComparison.Ordinal))
                {
                    offenders.Add(
                        $"{Path.GetRelativePath(FindRepositoryRoot(), file)} declares forbidden identifier {identifier}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            offenders.Count == 0 ? null : "Verdict-authority identifiers found:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Guard_Exemptions_Are_Discoverable_By_Marker_And_No_Other_File_Uses_Them()
    {
        // Two things are asserted: the files that must name what they forbid
        // actually carry the marker (otherwise the runtime scan would flag
        // them), and no other file sneaks in an exemption (otherwise a silent
        // skip would defeat the check above).
        var marked = MarkedSourceFiles()
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var approved = new[] { "GovernanceService.cs", "McpVerdictAuthorityTests.cs" };

        foreach (var name in approved)
        {
            Assert.Contains(name, marked);
        }

        var unapproved = marked.Except(approved, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.True(
            unapproved.Count == 0,
            unapproved.Count == 0
                ? null
                : "Unapproved guard-marker exemptions: " + string.Join(", ", unapproved));
    }

    [Fact]
    public void Runtime_Pattern_List_Covers_The_Baseline()
    {
        // Two directions of coverage: the runtime scan must contain every
        // identifier this baseline declares (a shrunk production list fails
        // here), and the production list may contain more, because extras are
        // the runtime scan's own widening — it uses the exact list.
        var runtime = HalalChain.Mcp.Services.GovernanceService.ForbiddenPatterns;
        var missing = ForbiddenIdentifiers.Except(runtime, StringComparer.Ordinal).ToList();

        Assert.True(
            missing.Count == 0,
            missing.Count == 0
                ? null
                : "GovernanceService.ForbiddenPatterns no longer covers: " + string.Join(", ", missing));
    }

    [Fact]
    public void Guard_Marker_Constant_Matches_The_Runtime_Scan()
    {
        // The literal marker is written into this file's header — that is what
        // makes the runtime scan skip it — so the constant here and the
        // constant there must be the same string, or the exemption silently
        // stops working.
        Assert.Equal(HalalChain.Mcp.Services.GovernanceService.GuardMarker, GuardMarker);
    }
}