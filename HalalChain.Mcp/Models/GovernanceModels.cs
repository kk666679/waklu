namespace HalalChain.Mcp.Models;

// ──────────────────────────────────────────────────────────────────────────
// Governance-introspection models (Wave 2).
//
// These describe *documents and declarations that already exist on disk*:
// principles, decisions, skill manifests, and the machinery that keeps a
// compliance verdict away from non-deterministic components. Nothing here
// grants or records authority — it reports where authority already lives.
// ──────────────────────────────────────────────────────────────────────────

/// <summary>A numbered principle, e.g. <c>P12 — Skills Cannot Bypass Governance</c>.</summary>
public class PrincipleInfo
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public int Line { get; set; }
}

/// <summary>The principles document, or the reason it could not be found.</summary>
public class PrinciplesDocument
{
    /// <summary>Root-relative path of the document that was parsed, when one was found.</summary>
    public string? Source { get; set; }

    /// <summary>Every path that was probed, so an empty result is actionable.</summary>
    public IReadOnlyList<string> Searched { get; set; } = [];

    public IReadOnlyList<PrincipleInfo> Principles { get; set; } = [];
}

/// <summary>One Architecture Decision Record.</summary>
public class AdrInfo
{
    public string Id { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string RelativePath { get; set; } = "";
}

/// <summary>The ADR index, plus the directories that were probed.</summary>
public class AdrIndex
{
    public string? Directory { get; set; }
    public IReadOnlyList<string> Searched { get; set; } = [];

    /// <summary>Mutable so the indexer can build it incrementally; treat as a list.</summary>
    public List<AdrInfo> Adrs { get; set; } = [];
}

/// <summary>
/// A published skill. Two conventions are recognised: a <c>skills/&lt;slug&gt;/manifest.yaml</c>
/// package, and a <c>.clinerules/&lt;slug&gt;.md</c> steering document, which is
/// where this repository actually keeps its skills today.
/// </summary>
public class SkillInfo
{
    public string Slug { get; set; } = "";
    public string Source { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>The skill index, plus the roots that were probed.</summary>
public class SkillIndex
{
    public IReadOnlyList<string> Searched { get; set; } = [];

    /// <summary>Mutable so the indexer can merge two conventions into one list.</summary>
    public List<SkillInfo> Skills { get; set; } = [];
}

/// <summary>
/// A single file lookup. <see cref="Found"/> distinguishes "the document says
/// nothing about this" from "the document is not there", which are different
/// problems with different fixes.
/// </summary>
public class DocumentLookup
{
    public bool Found { get; set; }
    public string? RelativePath { get; set; }
    public string? Content { get; set; }

    /// <summary>Every path that was probed.</summary>
    public IReadOnlyList<string> Searched { get; set; } = [];

    /// <summary>Set when the request was rejected before any lookup, e.g. a malformed id.</summary>
    public string? Error { get; set; }
}

/// <summary>A name that would hand verdict authority to something non-deterministic.</summary>
public class VerdictAuthorityViolation
{
    public string Pattern { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public int Line { get; set; }
}

/// <summary>
/// Result of the verdict-authority scan.
///
/// <see cref="Clean"/> is a *hint*, not a proof: a name is not a capability, and
/// a violation-free scan is not an audit. The count of files actually read is
/// published for the same reason — a scan of zero files must never read as
/// "clean".
/// </summary>
public class VerdictAuthorityReport
{
    public bool Clean { get; set; }
    public int ScannedFiles { get; set; }

    /// <summary>Guard implementations skipped by design (they must name what they forbid).</summary>
    public int SkippedGuardFiles { get; set; }

    public IReadOnlyList<string> Patterns { get; set; } = [];

    /// <summary>Mutable so the scan can append each match as it is found.</summary>
    public List<VerdictAuthorityViolation> Violations { get; set; } = [];
}

/// <summary>A workflow or state machine that participates in governance.</summary>
public class GovernanceWorkflowInfo
{
    public string Source { get; set; } = "";
    public string Kind { get; set; } = "";
    public string RelativePath { get; set; } = "";
}
