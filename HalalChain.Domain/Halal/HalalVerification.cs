namespace HalalChain.Domain.Halal;

/// <summary>
/// A deterministic verification result produced by the Policy Engine.
/// AI agents collect evidence; this record stores the final compliance decision.
/// </summary>
public sealed class HalalVerification
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public ComplianceStatus ComplianceStatus { get; set; } = ComplianceStatus.Unverified;
    public string PolicyVersion { get; set; } = "MY-v3";
    public string Jurisdiction { get; set; } = "MY";
    public string[] ReasonCodes { get; set; } = [];
    public string[] MissingEvidence { get; set; } = [];
    public bool RequiresHumanReview { get; set; }
    public DateTimeOffset VerifiedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<VerificationEvidence> Evidences { get; set; } = [];
    public List<VerificationAudit> Audits { get; set; } = [];
}

/// <summary>
/// Individual evidence collected by AI agents during halal verification.
/// Each evidence item has a type (ingredient, certificate, supplier, document),
/// a source, and a confidence score.
/// </summary>
public sealed class VerificationEvidence
{
    public Guid Id { get; set; }
    public Guid VerificationId { get; set; }
    public string EvidenceType { get; set; } = string.Empty; // ingredient, certificate, supplier, document
    public string Source { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTimeOffset CollectedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Append-only audit trail for verification decisions.
/// Records every action taken on a verification record.
/// </summary>
public sealed class VerificationAudit
{
    public Guid Id { get; set; }
    public Guid VerificationId { get; set; }
    public string Action { get; set; } = string.Empty; // e.g. "DECISION_RECORDED", "EVIDENCE_ADDED"
    public string Actor { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
