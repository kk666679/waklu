// halalchain:verdict-authority-guard
// AutoclawStructureTests
//
// Structural guardrails for the .autoclaw agent-orchestration tree.
//
// These rules are the enforcement layer for the .autoclaw restructure. The
// sealed tawheed boundary (P2) and the no-verdict rule (P3) are the two that
// matter most, and both are asserted here against the same pattern list and the
// same guard-marker convention the MCP server enforces at runtime — one
// definition, read reflectively from HalalChain.Mcp.Services.GovernanceService,
// so the two cannot drift.
//
// Every test in this file fails loudly when the thing it guards is missing.
// A structural test that passes because the directory it was looking for does
// not exist is not coverage; it is a hole with a green tick on it.

using System.Reflection;
using System.Text.RegularExpressions;
using HalalChain.Mcp.Services;
using Xunit;

namespace HalalChain.Architecture.Tests.Rules;

public sealed partial class AutoclawStructureTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Locating the tree
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Repository root. HALALCHAIN_SOLUTION_ROOT wins when set; otherwise we
    /// walk up from the test binary until we find the solution file.
    ///
    /// The doc's original fallback was <c>AppContext.BaseDirectory</c>, which
    /// would have pointed at bin/Release/net10.0 — every test below would have
    /// found nothing to scan and passed vacuously. Resolving the root is not an
    /// optional convenience here; it is what makes the rest of the file mean
    /// anything.
    /// </summary>
    private static string Root
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("HALALCHAIN_SOLUTION_ROOT");
            if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
            {
                return Path.GetFullPath(configured);
            }

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, ".autoclaw")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate the HalalChain repository root. Neither " +
                "HALALCHAIN_SOLUTION_ROOT nor any parent of " +
                $"{AppContext.BaseDirectory} contains .autoclaw/. Set " +
                "HALALCHAIN_SOLUTION_ROOT to the solution root.");
        }
    }

    private static string Auto => Path.Combine(Root, ".autoclaw");

    private static string At(string relative) => Path.Combine(Auto, relative.Replace('/', Path.DirectorySeparatorChar));

    // ────────────────────────────────────────────────────────────────────────
    // 1. The tree exists
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Autoclaw_Tree_Is_Scaffolded()
    {
        var required = new[]
        {
            "README.md",
            "MANIFEST.yaml",
            "PRINCIPLES.md",
            "CONTROLS.yaml",
            "agents/evidence-collector.yaml",
            "anchors/README.md",
            "byok/README.md",
            "compliance/README.md",
            "governance/README.md",
            "mcp/README.md",
            "tawheed/SEALED",
            "tawheed/README.md",
            "tawheed/api/evaluate.md",
            "tawheed/request.schema.json",
            "tawheed/response.schema.json",
            "tenants/README.md"
        };

        var missing = required
            .Where(r => !File.Exists(At(r)))
            .Select(r => r)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "The .autoclaw tree is incomplete. Run ./scripts/init-autoclaw.ps1. " +
            "Missing:\n" + string.Join("\n", missing));
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. P2 — tawheed is sealed
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Tawheed_Directory_Is_Sealed()
    {
        var sealedMarker = At("tawheed/SEALED");

        Assert.True(
            File.Exists(sealedMarker),
            "tawheed/SEALED marker missing. The sealed deterministic boundary " +
            "is asserted by the presence of this file; without it the boundary " +
            "is a convention and nothing more.");

        var manifest = RequireFile(At("MANIFEST.yaml"));

        // The marker alone proves nothing if the machine-readable index claims
        // the directory is writable. Both have to agree.
        Assert.Contains("sealed: true", manifest, StringComparison.Ordinal);
        Assert.Contains("writers: []", manifest, StringComparison.Ordinal);
        Assert.Contains("callers: [workflows/*]", manifest, StringComparison.Ordinal);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. P3 — no verdict authority outside tawheed
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Files under .autoclaw that legitimately name the forbidden identifiers.
    ///
    /// Each one is in the business of describing the ban — the catalogue, the
    /// machine-readable manifest, the governance policy that enforces it, the
    /// hooks that check for it, the MCP denylist, and the sealed boundary
    /// itself. A rule that cannot be stated without containing the thing it
    /// forbids has to be exempted somewhere, and an enumerated exemption is
    /// auditable in a way that a blanket "skip this directory" is not.
    /// </summary>
    private static readonly string[] AutoclawExclusions =
    {
        "CONTROLS.yaml",
        "MANIFEST.yaml",
        "PRINCIPLES.md",
        "governance/",
        "hooks/",
        "mcp/policies/",
        "tawheed/"
    };

    [Fact]
    public void No_Forbidden_Verdict_Pattern_Outside_Tawheed()
    {
        var patterns = ForbiddenPatterns();
        var marker = GuardMarker();
        var violations = new List<string>();
        var scanned = 0;

        // Scope 1: the .autoclaw tree, across every extension the restructure
        // introduces. This is new surface and nothing else scans it.
        foreach (var file in EnumerateText(At(".")))
        {
            var relative = Path.GetRelativePath(Auto, file).Replace('\\', '/');
            if (AutoclawExclusions.Any(e => relative.StartsWith(e, StringComparison.Ordinal)))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            scanned++;
            violations.AddRange(Matches(patterns, text, relative));
        }

        // Scope 2: every .cs file in the solution, matching the runtime guard's
        // own scope. The .cs scan is what catches a SetVerdict method written in
        // the platform rather than in a config file.
        foreach (var file in EnumerateText(Root, ".cs"))
        {
            var text = File.ReadAllText(file);
            if (text.Contains(marker, StringComparison.Ordinal))
            {
                continue;
            }

            var relative = Path.GetRelativePath(Root, file);
            scanned++;
            violations.AddRange(Matches(patterns, text, relative));
        }

        Assert.True(
            scanned > 0,
            "The verdict-authority scan read zero files. That is not a clean " +
            "result, it is a broken root path or a broken file filter, and the " +
            "difference matters.");

        Assert.True(
            violations.Count == 0,
            $"Forbidden verdict-authority identifiers found outside the sealed " +
            $"boundary. These are the names that would give something other " +
            $"than the Policy Engine the power to conclude (P2, P3). " +
            $"Files scanned: {scanned}. Offenders:\n" +
            string.Join("\n", violations));
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. P12 — skills are governed
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Every_Skill_Has_A_Manifest()
    {
        var published = At("skills/published");
        Assert.True(Directory.Exists(published), "skills/published/ is missing.");

        var skills = Directory.EnumerateDirectories(published).ToList();
        Assert.True(
            skills.Count > 0,
            "skills/published/ is empty. At least one governed skill must exist " +
            "for this rule to guard anything.");

        foreach (var skill in skills)
        {
            var name = Path.GetFileName(skill);
            var manifest = Path.Combine(skill, "manifest.yaml");

            Assert.True(
                File.Exists(manifest),
                $"Skill '{name}' has no manifest.yaml. A skill directory " +
                "without a manifest cannot be governed: no declared tools, no " +
                "declared budget, no record of what it passed.");
        }
    }

    [Fact]
    public void Published_Skills_Do_Not_Emit_Verdicts()
    {
        var published = At("skills/published");
        var offenders = new List<string>();

        foreach (var manifest in Directory.EnumerateFiles(published, "manifest.yaml", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(manifest);
            var name = Path.GetFileName(Path.GetDirectoryName(manifest)!);

            var emits = Sequence(lines, "emits:");
            var doesNot = Sequence(lines, "does_not_emit:");

            if (emits.Contains("ComplianceVerdict", StringComparer.Ordinal))
            {
                offenders.Add($"{name}: emits ComplianceVerdict");
            }

            if (!doesNot.Contains("ComplianceVerdict", StringComparer.Ordinal))
            {
                offenders.Add($"{name}: does_not_emit does not name ComplianceVerdict");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "A published skill must declare ComplianceVerdict in does_not_emit. " +
            "A skill that concludes is a skill whose output is indistinguishable " +
            "from a real evaluation, and nothing downstream can tell them apart. " +
            "Offenders:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Staging_Skills_Are_Not_Referenced_By_Agents()
    {
        var staging = At("skills/staging");
        if (!Directory.Exists(staging))
        {
            return;
        }

        var offenders = new List<string>();

        foreach (var agent in Directory.EnumerateFiles(At("agents"), "*.yaml"))
        {
            var text = File.ReadAllText(agent);
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                {
                    var skill = trimmed[2..].Trim();
                    if (Path.Combine(Auto, "skills", "staging", skill).Length > 0 &&
                        Directory.Exists(Path.Combine(Auto, "skills", "staging", skill)))
                    {
                        offenders.Add($"{Path.GetFileName(agent)} -> {skill}");
                    }
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "An agent persona references a skill under skills/staging/. Staging " +
            "is inert by design; a reference to it is a load-time failure waiting " +
            "to be discovered in production. Offenders:\n" +
            string.Join("\n", offenders));
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. P2/P3 at the MCP boundary
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Allowlist_Matches_Exposed_Tools()
    {
        var exposed = ExposedToolNames();
        var allowed = NamesInSection(File.ReadAllLines(At("mcp/policies/tool-allowlist.yaml")), "allowed:");

        Assert.True(
            exposed.Count > 0,
            "No tools found in HalalChain.Mcp/Tools. The allowlist cannot be " +
            "cross-checked against an empty server.");

        var missing = exposed.Except(allowed, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var extra = allowed.Except(exposed, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();

        Assert.True(
            missing.Count == 0,
            "HalalChain.Mcp exposes tools that the allowlist does not list. " +
            "Because the allowlist is a whitelist with default_action: deny, " +
            "these are unreachable through the fleet — a server that is " +
            "simultaneously shipped and refused. Add them to " +
            "mcp/policies/tool-allowlist.yaml. Missing:\n" +
            string.Join("\n", missing));

        Assert.True(
            extra.Count == 0,
            "mcp/policies/tool-allowlist.yaml lists tools the server does not " +
            "implement. Either the tool was removed from the server or the " +
            "allowlist is aspirational. Both are drift. Extra:\n" +
            string.Join("\n", extra));
    }

    [Fact]
    public void Denied_Tools_Are_Not_Exposed_By_The_Server()
    {
        var denied = NamesInSection(File.ReadAllLines(At("mcp/policies/tool-allowlist.yaml")), "denied:");
        var exposed = ExposedToolNames();

        Assert.True(
            denied.Count > 0,
            "The denylist is empty. A denylist with no entries means the policy " +
            "file is documenting an intention rather than enforcing one.");

        var overlap = denied.Intersect(exposed, StringComparer.Ordinal).ToList();

        Assert.True(
            overlap.Count == 0,
            "HalalChain.Mcp implements tools the policy denies. The policy layer " +
            "refuses them before dispatch, which is good, but a server that " +
            "actually implements a verdict-writing tool is a second path to a " +
            "verdict that does not pass through the sealed boundary. Overlap:\n" +
            string.Join("\n", overlap));
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. P6 — tenant isolation is declared at every layer
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Tenant_Isolation_Keys_Are_Declared()
    {
        var model = RequireFile(At("tenants/model.yaml"));
        var required = new[] { "database:", "cache:", "blob:", "search:", "mcp:" };

        var missing = required
            .Where(key => !model.Contains("\n  " + key, StringComparison.Ordinal))
            .ToList();

        Assert.True(
            missing.Count == 0,
            "tenants/model.yaml does not declare an isolation key for: " +
            string.Join(", ", missing) + ". A layer without an isolation key is " +
            "a layer where one tenant's data is indistinguishable from another's.");
    }

    [Fact]
    public void Every_Agent_Is_Tenant_Scoped()
    {
        var personas = Directory.EnumerateFiles(At("agents"), "*.yaml").ToList();
        Assert.True(personas.Count > 0, "agents/ contains no personas.");

        var unscoped = personas
            .Where(p => !File.ReadAllText(p).Contains("tenant_scoped: true", StringComparison.Ordinal))
            .Select(p => Path.GetFileName(p))
            .ToList();

        Assert.True(
            unscoped.Count == 0,
            "Every agent persona must set tenant_scoped: true. An agent without " +
            "a tenant has no evidence scope and no place in the isolation model. " +
            "Offenders:\n" + string.Join("\n", unscoped));
    }

    [Fact]
    public void Every_Agent_Declares_What_It_Cannot_Do()
    {
        var personas = Directory.EnumerateFiles(At("agents"), "*.yaml").ToList();
        var offenders = new List<string>();

        foreach (var persona in personas)
        {
            var lines = File.ReadAllLines(persona);
            var cannot = Sequence(lines, "cannot:");

            if (cannot.Count == 0)
            {
                offenders.Add(Path.GetFileName(persona));
            }
        }

        Assert.True(
            offenders.Count == 0,
            "An agent persona with an empty `cannot` list is one that has not " +
            "been reviewed yet. The `cannot` list is the enforced half of the " +
            "declaration; `can` alone says nothing about the limits. Offenders:\n" +
            string.Join("\n", offenders));
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. P7/P11 — no credential values anywhere
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Byok_Contains_Refs_Not_Secrets()
    {
        var valueShapes = new[]
        {
            @"\b(?:sk|pk)-[A-Za-z0-9]{20,}\b",
            @"\bgh[pousr]_[A-Za-z0-9]{20,}\b",
            @"\b0x[a-fA-F0-9]{64}\b",
            @"\bAKIA[0-9A-Z]{16}\b",
            @"\bbyok:[a-z0-9-]+:[A-Za-z0-9]{8,}\b",
            @"-----BEGIN [A-Z ]*PRIVATE KEY-----",
            @"(?i)postgres(?:ql)?://[^\s:]+:[^\s@]+@"
        };

        var offenders = new List<string>();

        foreach (var file in EnumerateText(At("byok")))
        {
            var text = File.ReadAllText(file);
            var relative = Path.GetRelativePath(Auto, file);

            foreach (var shape in valueShapes)
            {
                if (Regex.IsMatch(text, shape))
                {
                    offenders.Add($"{relative} matches {shape}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "A credential value appears under .autoclaw/byok/. That directory " +
            "holds sealed references and lifecycle policy, never values. " +
            "Offenders:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Redaction_Config_Consumes_The_Secret_Pattern_List()
    {
        var redaction = RequireFile(At("observability/redaction.yaml"));
        var secrets = File.Exists(At("security/secrets/never-log.yaml"));

        Assert.True(
            secrets,
            "security/secrets/never-log.yaml is missing. It is the single " +
            "definition of what must never be logged.");

        Assert.Contains(
            "security/secrets/never-log.yaml",
            redaction,
            StringComparison.Ordinal);

        // Redaction has to happen before the record is written. A pipeline that
        // redacts on read leaves the secret on disk for the whole interval
        // between the write and the read.
        Assert.Contains("before-write", redaction, StringComparison.OrdinalIgnoreCase);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 8. Governance — the certification machine is internally consistent
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Certification_Transitions_Reference_Valid_States()
    {
        var lines = File.ReadAllLines(At("governance/certification/transitions.yaml"));

        var states = Section(lines, "states:")
            .Select(l => IdMatch(l))
            .Where(id => id is not null)
            .Select(id => id!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(states.Count > 0, "No states parsed from transitions.yaml.");

        var transitions = ParseTransitions(lines);
        Assert.True(
            transitions.Length > 0,
            "No transitions parsed from transitions.yaml. The state machine is " +
            "the enforcement for P9; an unparseable one is not enforced.");

        var bad = transitions
            .Where(t => !states.Contains(t.From) || !states.Contains(t.To))
            .Select(t => $"{t.Id}: {t.From} -> {t.To}")
            .ToList();

        Assert.True(
            bad.Count == 0,
            "Transitions reference states the machine does not declare. " +
            "Offenders:\n" + string.Join("\n", bad));
    }

    [Fact]
    public void Certification_Guards_Are_Documented()
    {
        var lines = File.ReadAllLines(At("governance/certification/transitions.yaml"));
        var guards = GuardNames(lines);

        Assert.True(
            guards.Count > 0,
            "No guards parsed from transitions.yaml. A transition that names a " +
            "guard nobody can look up is a transition nobody can audit.");

        var doc = RequireFile(At("governance/certification/guards.md"));

        var undocumented = guards
            .Where(g => !doc.Contains(g, StringComparison.Ordinal))
            .OrderBy(g => g, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            undocumented.Count == 0,
            "Guards are named in transitions.yaml but not defined in guards.md. " +
            "A guard without a documented source of truth cannot be verified by " +
            "anyone reviewing a transition. Undocumented:\n" +
            string.Join("\n", undocumented));
    }

    [Fact]
    public void Authority_Transitions_Are_A_Bipartite_Subset_Of_The_Machine()
    {
        var lines = File.ReadAllLines(At("governance/certification/transitions.yaml"));
        var machine = ParseTransitions(lines).Select(t => t.Id).ToHashSet(StringComparer.Ordinal);

        var authority = At("governance/certification/authorities/certification-body.yaml");
        var authorityLines = File.ReadAllLines(authority);

        var may = Section(authorityLines, "may_transition:")
            .Select(Scalar)
            .Where(s => s.Length > 0)
            .ToList();

        Assert.True(may.Count > 0, "No transitions parsed from the authority file.");

        var unknown = may.Where(t => !machine.Contains(t)).ToList();

        Assert.True(
            unknown.Count == 0,
            "The certification-body authority claims transitions the machine " +
            "does not declare. Authority that is not in the machine does not " +
            "exist. Unknown:\n" + string.Join("\n", unknown));

        // The consultant is the role the whole certification scheme is built
        // around keeping out of the decision, so the assertion is explicit
        // rather than implied by the absence of an entry.
        var mayNot = Section(authorityLines, "may_not_transition:")
            .Select(Scalar)
            .Where(s => s.Length > 0)
            .ToList();

        Assert.Contains("T06", mayNot);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 9. P10 — anchors produce receipts, not decisions
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Anchors_Do_Not_Declare_Verdict_Types()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateText(At("anchors")))
        {
            var text = File.ReadAllText(file);
            foreach (var banned in new[] { "may_read_status_from_chain: true", "receipt_is_rule_input: true" })
            {
                if (text.Contains(banned, StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetRelativePath(Auto, file)}: {banned}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "The anchor subsystem is configured to read a compliance status from " +
            "a chain, or to feed a receipt into a rule. A receipt proves a hash " +
            "has not changed; it says nothing about validity or compliance. " +
            "P10 exists to keep that distinction from eroding. Offenders:\n" +
            string.Join("\n", offenders));
    }

    // ────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────

    private static string RequireFile(string path)
    {
        Assert.True(
            File.Exists(path),
            $"Required .autoclaw file is missing: {Path.GetRelativePath(Root, path)}");

        return File.ReadAllText(path);
    }

    /// <summary>
    /// The forbidden-identifier list, read from the MCP server rather than
    /// restated. Two copies would drift, and a guardrail whose pattern set has
    /// quietly widened reports clean while enforcing nothing.
    /// </summary>
    private static string[] ForbiddenPatterns()
    {
        var field = typeof(GovernanceService).GetField(
            "ForbiddenPatterns",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.True(
            field is not null,
            "HalalChain.Mcp.Services.GovernanceService.ForbiddenPatterns not " +
            "found. If it was renamed or removed, the verdict-authority scan " +
            "has nothing to enforce and this file has to be updated with it — " +
            "not left passing quietly.");

        return ((string[])field.GetValue(null)!)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();
    }

    private static string GuardMarker()
    {
        var field = typeof(GovernanceService).GetField(
            "GuardMarker",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.True(
            field is not null,
            "HalalChain.Mcp.Services.GovernanceService.GuardMarker not found. " +
            "Without it, files that legitimately declare the forbidden list " +
            "cannot be exempted, and the scan always reports itself.");

        return (string)field.GetValue(null)!;
    }

    private static HashSet<string> ExposedToolNames()
    {
        var toolsDir = Path.Combine(Root, "HalalChain.Mcp", "Tools");
        Assert.True(Directory.Exists(toolsDir), "HalalChain.Mcp/Tools not found.");

        var names = new HashSet<string>(StringComparer.Ordinal);
        var nameExpression = new Regex(@"Name\s*=>\s*""(halalchain_[a-z_]+)""", RegexOptions.Compiled);

        foreach (var file in Directory.EnumerateFiles(toolsDir, "*.cs", SearchOption.AllDirectories))
        {
            foreach (Match match in nameExpression.Matches(File.ReadAllText(file)))
            {
                names.Add(match.Groups[1].Value);
            }
        }

        return names;
    }

    private static IEnumerable<string> Matches(IReadOnlyList<string> patterns, string text, string relative)
    {
        foreach (var pattern in patterns)
        {
            var index = text.IndexOf(pattern, StringComparison.Ordinal);
            while (index >= 0)
            {
                var line = 1 + text[..index].Count(c => c == '\n');
                yield return $"{relative}:{line} contains '{pattern}'";
                index = text.IndexOf(pattern, index + pattern.Length, StringComparison.Ordinal);
            }
        }
    }

    private static readonly string[] PrunedDirectories =
    {
        "bin", "obj", "node_modules", ".git", ".venv", "venv",
        "dist", "out", "TestResults", ".autoclaw"
    };

    private static readonly string[] TextExtensions =
    {
        ".cs", ".yaml", ".yml", ".json", ".md", ".py", ".ts", ".js"
    };

    /// <summary>
    /// Enumerate readable text files, pruning build output. An unreadable file
    /// is skipped rather than thrown on: a locked file should not fail a
    /// structural rule.
    /// </summary>
    private static IEnumerable<string> EnumerateText(string root, params string[] extensionFilter)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        var extensions = extensionFilter.Length > 0
            ? new HashSet<string>(extensionFilter, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(TextExtensions, StringComparer.OrdinalIgnoreCase);

        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            string[] files;
            string[] directories;
            try
            {
                files = Directory.GetFiles(current);
                directories = Directory.GetDirectories(current);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                continue;
            }

            foreach (var file in files)
            {
                if (extensions.Contains(Path.GetExtension(file)))
                {
                    yield return file;
                }
            }

            foreach (var directory in directories)
            {
                var name = Path.GetFileName(directory);
                if (!PrunedDirectories.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    pending.Push(directory);
                }
            }
        }
    }

    // ── minimal YAML-subset readers ─────────────────────────────────────────
    // The .autoclaw policy files are a deliberately narrow YAML subset: block
    // mappings and block sequences of scalars, plus inline [a, b] lists. Parsing
    // that directly keeps the test project free of a YAML dependency, and a
    // dependency added only to read three files is a dependency to keep
    // patching. The readers below are strict — they fail the test they serve
    // rather than returning an empty collection on something they did not
    // understand.

    /// <summary>Lines belonging to a top-level <c>key:</c> block.</summary>
    private static List<string> Section(IReadOnlyList<string> lines, string header)
    {
        var result = new List<string>();
        var start = -1;

        for (var i = 0; i < lines.Count && start < 0; i++)
        {
            if (lines[i].TrimEnd() == header)
            {
                start = i + 1;
            }
        }

        if (start < 0)
        {
            return result;
        }

        for (var i = start; i < lines.Count; i++)
        {
            var line = lines[i].TrimEnd();
            if (line.Length == 0)
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]))
            {
                break; // next top-level key
            }

            result.Add(line);
        }

        return result;
    }

    /// <summary>Values of <c>- name: x</c> entries inside a top-level section.</summary>
    private static List<string> NamesInSection(IReadOnlyList<string> lines, string header)
    {
        var entry = new Regex(@"^\s*-\s+name:\s*(\S+)\s*$");

        return Section(lines, header)
            .Select(l => entry.Match(l))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value.Trim('"', '\''))
            .ToList();
    }

    /// <summary>
    /// A bare sequence item: leading dash removed, trailing inline comment
    /// removed, surrounding quotes removed.
    ///
    /// The .autoclaw policy files use trailing <c>#</c> comments as a normal
    /// convention — <c>- T06 # consultants/producers only</c> says something a
    /// bare <c>T06</c> cannot — so a reader that does not strip them compares
    /// an identifier against its own annotation and calls the two different.
    /// The comment must be preceded by whitespace, so a <c>#</c> inside a value
    /// survives; a hash inside a quoted string preceded by a space would not,
    /// which is the one case this reader does not model and none of these
    /// files use.
    /// </summary>
    private static string Scalar(string line)
    {
        var value = line.Trim().TrimStart('-').Trim();

        var comment = value.IndexOf(" #", StringComparison.Ordinal);
        if (comment >= 0)
        {
            value = value[..comment].TrimEnd();
        }

        return value.Trim('"', '\'');
    }

    /// <summary>
    /// The value of <c>key:</c>, whether written inline as <c>[a, b]</c> or as a
    /// block sequence on the following lines.
    /// </summary>
    private static List<string> Sequence(IReadOnlyList<string> lines, string key)
    {
        var result = new List<string>();
        var prefix = key.TrimEnd(':');
        var keyPattern = new Regex(@"^(\s*)" + Regex.Escape(prefix) + @":\s*(.*)$");

        for (var i = 0; i < lines.Count; i++)
        {
            var match = keyPattern.Match(lines[i]);
            if (!match.Success)
            {
                continue;
            }

            var inline = match.Groups[2].Value.Trim();
            if (inline.StartsWith('[') && inline.EndsWith(']'))
            {
                result.AddRange(InlineList(inline[1..^1]));
                return result;
            }

            if (inline.Length > 0)
            {
                result.Add(inline.Trim('"', '\''));
                return result;
            }

            var indent = match.Groups[1].Value.Length;
            for (var j = i + 1; j < lines.Count; j++)
            {
                var line = lines[j];
                if (line.Trim().Length == 0)
                {
                    continue;
                }

                var itemIndent = line.Length - line.TrimStart().Length;
                if (itemIndent <= indent)
                {
                    break;
                }

                var trimmed = line.Trim();
                if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                {
                    result.Add(trimmed[2..].Trim().Trim('"', '\''));
                }
            }

            return result;
        }

        return result;
    }

    private static List<string> InlineList(string body) =>
        body.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.Trim('"', '\''))
            .Where(s => s.Length > 0)
            .ToList();

    private static string? IdMatch(string line)
    {
        var match = new Regex(@"^\s*-\s+id:\s*(\S+)\s*$").Match(line);
        return match.Success ? match.Groups[1].Value.Trim('"', '\'') : null;
    }

    private static (string Id, string From, string To)[] ParseTransitions(IReadOnlyList<string> lines)
    {
        var transitions = Section(lines, "transitions:");

        var idPattern = new Regex(@"^  - id:\s*(\S+)\s*$");
        var fromPattern = new Regex(@"^    from:\s*(\S+)\s*$");
        var toPattern = new Regex(@"^    to:\s*(\S+)\s*$");

        var result = new List<(string, string, string)>();
        string? id = null, from = null, to = null;

        void Flush()
        {
            if (id is not null && from is not null && to is not null)
            {
                result.Add((id, from, to));
            }

            id = from = to = null;
        }

        foreach (var line in transitions)
        {
            var trimmed = line.TrimEnd();
            if (trimmed.Trim().Length == 0)
            {
                continue;
            }

            var idMatch = idPattern.Match(trimmed);
            if (idMatch.Success)
            {
                Flush();
                id = idMatch.Groups[1].Value;
                continue;
            }

            var fromMatch = fromPattern.Match(trimmed);
            if (fromMatch.Success)
            {
                from = fromMatch.Groups[1].Value.Trim('"', '\'');
                continue;
            }

            var toMatch = toPattern.Match(trimmed);
            if (toMatch.Success)
            {
                to = toMatch.Groups[1].Value.Trim('"', '\'');
            }
        }

        Flush();
        return result.ToArray();
    }

    /// <summary>
    /// Every guard name under any <c>guards:</c> or <c>additional_guards:</c>
    /// key. Guards whose "name" is an expression (for example
    /// <c>evidence_completeness &gt;= 0.9</c>) are returned whole, which is what
    /// the documentation file has to contain.
    /// </summary>
    private static List<string> GuardNames(IReadOnlyList<string> lines)
    {
        var header = new Regex(@"^(\s*)(guards|additional_guards):\s*$");
        var result = new List<string>();

        for (var i = 0; i < lines.Count; i++)
        {
            var match = header.Match(lines[i]);
            if (!match.Success)
            {
                continue;
            }

            var indent = match.Groups[1].Value.Length;

            for (var j = i + 1; j < lines.Count; j++)
            {
                var line = lines[j];
                if (line.Trim().Length == 0)
                {
                    continue;
                }

                var itemIndent = line.Length - line.TrimStart().Length;
                if (itemIndent <= indent)
                {
                    break;
                }

                var trimmed = line.Trim();
                if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                {
                    result.Add(trimmed[2..].Trim());
                }
            }
        }

        return result;
    }
}
