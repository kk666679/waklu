using System.Text.RegularExpressions;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace HalalChain.Mcp.Services;

/// <summary>
/// Reads the repository's governance surface: principles, ADRs, skills, the
/// service manifest, and the state machines that carry governance transitions.
///
/// Everything here is a read. The type has no member that writes a document and
/// no member that decides an outcome — the verdict-authority scan is itself a
/// text search, and it publishes the number of files it read so an empty result
/// cannot be mistaken for a clean bill of health.
/// </summary>
internal sealed partial class GovernanceService : IGovernanceService
{
    // ────────────────────────────────────────────────────────────────────────
    // halalchain:verdict-authority-guard
    //
    // This file declares the forbidden-pattern list, so it necessarily contains
    // every pattern it forbids. The scan skips files carrying this marker —
    // otherwise the guard would always report itself, and a check that always
    // fails is a check nobody reads. The marker is what makes the skip
    // auditable: grep for it to find every file that is exempt.
    // ────────────────────────────────────────────────────────────────────────

    // internal (not private) because HalalChain.Architecture.Tests reads it
    // via the InternalsVisibleTo in AssemblyInfo.cs: the architecture test
    // asserts its independent baseline against this list, so shrinking the
    // list here becomes a test failure instead of silent coverage loss.
    internal const string GuardMarker = "halalchain:verdict-authority-guard";

    /// <summary>
    /// Names that would give something other than the deterministic Policy
    /// Engine the power to decide a compliance outcome. Matched as raw text, so
    /// the scan errs toward reporting a candidate for a human to confirm.
    /// Internal so HalalChain.Architecture.Tests can assert its independent
    /// baseline against the exact list the runtime scan uses.
    /// </summary>
    internal static readonly string[] ForbiddenPatterns =
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

    private static readonly string[] PrincipleCandidates =
    [
        ".cline_inbox/PRINCIPLES.md",
        "PRINCIPLES.md",
        "docs/PRINCIPLES.md",
        "docs/architecture/PRINCIPLES.md",
        "AGENTS.md",
    ];

    private static readonly string[] AdrDirectories =
    [
        "docs/architecture/adr",
        "docs/adr",
    ];

    private static readonly string[] ControlsCandidates =
    [
        ".cline_inbox/manifests/controls.yaml",
        ".cline_inbox/controls.yaml",
        "controls.yaml",
        "docs/controls.yaml",
    ];

    private static readonly string[] ServiceManifestCandidates =
    [
        "service-manifest.yaml",
        "docs/service-manifest.yaml",
    ];

    /// <summary>Extensions that can carry a tawheed policy or workflow definition.</summary>
    private static readonly string[] WorkflowExtensions = [".py", ".yaml", ".yml", ".json", ".md"];

    private readonly SourceTreeScanner _tree;
    private readonly string _root;
    private readonly ILogger<GovernanceService> _logger;

    public GovernanceService(
        IPlatformDataService platformData,
        ILogger<GovernanceService> logger)
    {
        _logger = logger;
        _root = Path.GetFullPath(platformData.GetSolutionRoot());
        _tree = new SourceTreeScanner(_root);
    }

    public PrinciplesDocument GetPrinciples()
    {
        var document = new PrinciplesDocument { Searched = PrincipleCandidates };

        foreach (var candidate in PrincipleCandidates)
        {
            var path = Path.Combine(_root, candidate);
            if (!File.Exists(path))
            {
                continue;
            }

            var text = SourceTreeScanner.TryRead(path);
            if (text is null)
            {
                continue;
            }

            document.Source = candidate;
            document.Principles = PrincipleRegex().Matches(text)
                .Select(match => new PrincipleInfo
                {
                    Id = match.Groups["id"].Value.ToUpperInvariant(),
                    Title = match.Groups["title"].Value.Trim().TrimEnd('.', '—', '-', ':'),
                    Line = SourceTreeScanner.LineNumber(text, match.Index),
                })
                .ToList();

            // The first candidate that exists is authoritative; falling through
            // to a weaker document would silently mix two rule sets.
            break;
        }

        return document;
    }

    public AdrIndex ListAdrs()
    {
        var index = new AdrIndex { Searched = AdrDirectories };

        foreach (var candidate in AdrDirectories)
        {
            var directory = Path.Combine(_root, candidate);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            index.Directory = candidate;

            foreach (var file in Directory.GetFiles(directory, "*.md").OrderBy(f => f, StringComparer.Ordinal))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var match = AdrFileRegex().Match(name);
                var id = match.Success ? match.Groups["id"].Value : name;
                var slug = match.Success ? match.Groups["slug"].Value : name;
                var heading = FirstHeading(SourceTreeScanner.TryRead(file));

                index.Adrs.Add(new AdrInfo
                {
                    Id = id,
                    Slug = slug,
                    Title = heading ?? slug.Replace('-', ' '),
                    RelativePath = SourceTreeScanner.Normalise(Path.Combine(candidate, Path.GetFileName(file))),
                });
            }

            break;
        }

        return index;
    }

    public DocumentLookup GetAdr(string id)
    {
        var lookup = new DocumentLookup { Searched = AdrDirectories };

        if (string.IsNullOrWhiteSpace(id) || !SafeSegmentRegex().IsMatch(id))
        {
            lookup.Error = "ADR id must be alphanumeric with optional dashes or underscores.";
            return lookup;
        }

        foreach (var candidate in AdrDirectories)
        {
            var directory = Path.Combine(_root, candidate);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            // The id is matched against the file name only, and only after the
            // traversal-safe shape above has been enforced: a crafted id cannot
            // reach outside the ADR directory.
            var file = Directory.GetFiles(directory, "*.md")
                .FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).Equals(id, StringComparison.OrdinalIgnoreCase)
                    || Path.GetFileNameWithoutExtension(f).StartsWith(id + "-", StringComparison.OrdinalIgnoreCase));

            if (file is null)
            {
                return lookup;
            }

            lookup.Found = true;
            lookup.RelativePath = SourceTreeScanner.Normalise(Path.Combine(candidate, Path.GetFileName(file)));
            lookup.Content = SourceTreeScanner.TryRead(file);
            return lookup;
        }

        return lookup;
    }

    public SkillIndex ListSkills()
    {
        var index = new SkillIndex { Searched = ["skills/*/manifest.yaml", ".clinerules/*.md"] };

        // Convention 1: a packaged skill — skills/<slug>/manifest.yaml.
        var skillsDirectory = Path.Combine(_root, "skills");
        if (Directory.Exists(skillsDirectory))
        {
            foreach (var directory in Directory.GetDirectories(skillsDirectory).OrderBy(d => d, StringComparer.Ordinal))
            {
                var manifest = ManifestPath(directory);
                if (manifest is null)
                {
                    continue;
                }

                var text = SourceTreeScanner.TryRead(manifest) ?? "";
                index.Skills.Add(new SkillInfo
                {
                    Slug = Path.GetFileName(directory),
                    Source = "skills-manifest",
                    RelativePath = SourceTreeScanner.Normalise(
                        Path.Combine("skills", Path.GetFileName(directory), Path.GetFileName(manifest))),
                    Title = YamlValue(text, "name") ?? Path.GetFileName(directory),
                    Description = YamlValue(text, "description") ?? "",
                });
            }
        }

        // Convention 2: a steering document — .clinerules/<slug>.md. This is
        // where the repository's skills actually live today, so a scanner that
        // only understood convention 1 would report an empty skill surface and
        // be wrong about it.
        var rulesDirectory = Path.Combine(_root, ".clinerules");
        if (Directory.Exists(rulesDirectory))
        {
            foreach (var file in Directory.GetFiles(rulesDirectory, "*.md").OrderBy(f => f, StringComparer.Ordinal))
            {
                var text = SourceTreeScanner.TryRead(file) ?? "";
                index.Skills.Add(new SkillInfo
                {
                    Slug = Path.GetFileNameWithoutExtension(file),
                    Source = "clinerules",
                    RelativePath = SourceTreeScanner.Normalise(Path.Combine(".clinerules", Path.GetFileName(file))),
                    Title = FirstHeading(text) ?? Path.GetFileNameWithoutExtension(file),
                    Description = FirstQuote(text) ?? "",
                });
            }
        }

        return index;
    }

    public DocumentLookup GetSkill(string slug)
    {
        var lookup = new DocumentLookup
        {
            Searched = ["skills/<slug>/manifest.yaml", ".clinerules/<slug>.md"],
        };

        if (string.IsNullOrWhiteSpace(slug) || !SafeSegmentRegex().IsMatch(slug))
        {
            lookup.Error = "Skill slug must be alphanumeric with optional dashes or underscores.";
            return lookup;
        }

        var directory = Path.Combine(_root, "skills", slug);
        if (Directory.Exists(directory))
        {
            var manifest = ManifestPath(directory);
            if (manifest is not null)
            {
                lookup.Found = true;
                lookup.RelativePath = SourceTreeScanner.Normalise(
                    Path.Combine("skills", slug, Path.GetFileName(manifest)));
                lookup.Content = SourceTreeScanner.TryRead(manifest);
                return lookup;
            }
        }

        var steering = Path.Combine(_root, ".clinerules", slug + ".md");
        if (File.Exists(steering))
        {
            lookup.Found = true;
            lookup.RelativePath = SourceTreeScanner.Normalise(Path.Combine(".clinerules", slug + ".md"));
            lookup.Content = SourceTreeScanner.TryRead(steering);
        }

        return lookup;
    }

    public DocumentLookup GetControls()
    {
        var lookup = new DocumentLookup { Searched = ControlsCandidates };

        foreach (var candidate in ControlsCandidates)
        {
            var path = Path.Combine(_root, candidate);

            if (!File.Exists(path))
            {
                continue;
            }

            lookup.Found = true;
            lookup.RelativePath = candidate;
            lookup.Content = SourceTreeScanner.TryRead(path);
            return lookup;
        }

        return lookup;
    }

    public DocumentLookup GetServiceManifest()
    {
        var lookup = new DocumentLookup { Searched = ServiceManifestCandidates };

        foreach (var candidate in ServiceManifestCandidates)
        {
            var path = Path.Combine(_root, candidate);

            if (!File.Exists(path))
            {
                continue;
            }

            lookup.Found = true;
            lookup.RelativePath = candidate;
            lookup.Content = SourceTreeScanner.TryRead(path);
            return lookup;
        }

        return lookup;
    }

    public VerdictAuthorityReport VerifyNoVerdictAuthority()
    {
        var report = new VerdictAuthorityReport { Patterns = ForbiddenPatterns };

        foreach (var file in _tree.Enumerate(".cs"))
        {
            var text = SourceTreeScanner.TryRead(file);
            if (text is null)
            {
                continue;
            }

            // The guard's own declaration — this file, and the architecture
            // test that mirrors it — has to name what it forbids. Skipping it is
            // the only way the check can pass at all, so the skip is counted
            // rather than hidden.
            if (text.Contains(GuardMarker, StringComparison.Ordinal))
            {
                report.SkippedGuardFiles++;
                continue;
            }

            report.ScannedFiles++;

            foreach (var pattern in ForbiddenPatterns)
            {
                var index = text.IndexOf(pattern, StringComparison.Ordinal);

                while (index >= 0)
                {
                    report.Violations.Add(new VerdictAuthorityViolation
                    {
                        Pattern = pattern,
                        RelativePath = _tree.RelativePath(file),
                        Line = SourceTreeScanner.LineNumber(text, index),
                    });

                    index = text.IndexOf(pattern, index + pattern.Length, StringComparison.Ordinal);
                }
            }
        }

        report.Clean = report.Violations.Count == 0;

        _logger.LogDebug(
            "Verdict-authority scan read {Scanned} files, skipped {Skipped} guard files, found {Violations} matches",
            report.ScannedFiles,
            report.SkippedGuardFiles,
            report.Violations.Count);

        return report;
    }

    public IReadOnlyList<GovernanceWorkflowInfo> GetGovernanceWorkflows(string? projectName = null)
    {
        var workflows = new List<GovernanceWorkflowInfo>();

        // tawheed is the deterministic boundary, so the definitions that carry a
        // governance transition live there. It sits under a dot-directory, which
        // the solution-wide scanner prunes by design, hence the direct walk.
        var tawheed = Path.Combine(_root, ".halalchain", "tawheed");
        foreach (var file in EnumerateUnder(tawheed, WorkflowExtensions))
        {
            workflows.Add(new GovernanceWorkflowInfo
            {
                Source = "tawheed",
                Kind = KindOf(file),
                RelativePath = SourceTreeScanner.Normalise(Path.GetRelativePath(_root, file)),
            });
        }

        // Domain-side transitions (for example ProductStatusMachine) are state
        // machines in C#, and they are the ones that decide *status*, never a
        // compliance verdict.
        foreach (var file in _tree.Enumerate(".cs", projectName))
        {
            if (!Path.GetFileNameWithoutExtension(file).Contains("StateMachine", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            workflows.Add(new GovernanceWorkflowInfo
            {
                Source = "domain",
                Kind = "state-machine",
                RelativePath = _tree.RelativePath(file),
            });
        }

        return workflows
            .OrderBy(w => w.Source, StringComparer.Ordinal)
            .ThenBy(w => w.RelativePath, StringComparer.Ordinal)
            .ToList();
    }

    [GeneratedRegex(@"^\s*[-*]?\s*(?<id>P\d{1,3})\s*[—\-:]\s*(?<title>.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex PrincipleRegex();

    [GeneratedRegex(@"^(?<id>[0-9A-Za-z]+)-(?<slug>.+)$")]
    private static partial Regex AdrFileRegex();

    /// <summary>The skill manifest inside a package directory, either extension.</summary>
    private static string? ManifestPath(string directory)
    {
        var yaml = Path.Combine(directory, "manifest.yaml");
        if (File.Exists(yaml))
        {
            return yaml;
        }

        var yml = Path.Combine(directory, "manifest.yml");
        return File.Exists(yml) ? yml : null;
    }

    /// <summary>Reads a flat <c>key: value</c> pair from a small YAML document.</summary>
    private static string? YamlValue(string text, string key)
    {
        var match = Regex.Match(
            text,
            @"^\s*" + Regex.Escape(key) + @"\s*:\s*(?<value>.+?)\s*$",
            RegexOptions.Multiline);

        return match.Success ? match.Groups["value"].Value.Trim().Trim('"', '\'') : null;
    }

    /// <summary>The first Markdown <c># </c> heading, used as a document title.</summary>
    private static string? FirstHeading(string? text)
    {
        if (text is null)
        {
            return null;
        }

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                return trimmed[2..].Trim();
            }
        }

        return null;
    }

    /// <summary>The first Markdown block quote, which is where a steering document states its purpose.</summary>
    private static string? FirstQuote(string? text)
    {
        if (text is null)
        {
            return null;
        }

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("> ", StringComparison.Ordinal))
            {
                return trimmed[2..].Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Walks a directory outside the solution's C# surface (for example
    /// <c>.halalchain/tawheed</c>, which the dot-directory prune would skip).
    /// </summary>
    private static IEnumerable<string> EnumerateUnder(string directory, string[] extensions)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            string[] files;
            string[] children;

            try
            {
                files = Directory.GetFiles(current);
                children = Directory.GetDirectories(current);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);
                if (Array.Exists(extensions, e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return file;
                }
            }

            foreach (var child in children)
            {
                var name = Path.GetFileName(child);
                if (name is "__pycache__" or ".venv" or "venv" or "node_modules" or "bin" or "obj" or ".git" or "dist")
                {
                    continue;
                }

                pending.Push(child);
            }
        }
    }

    /// <summary>Coarse kind of a workflow definition, from its extension.</summary>
    private static string KindOf(string file) => Path.GetExtension(file).ToLowerInvariant() switch
    {
        ".py" => "python",
        ".yaml" or ".yml" => "yaml",
        ".json" => "json",
        ".md" => "documentation",
        _ => "file",
    };

    /// <summary>A single path segment: no separators, no traversal, no dots.</summary>
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9_\-]{0,63}$")]
    private static partial Regex SafeSegmentRegex();
}
