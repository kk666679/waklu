namespace HalalChain.Platform.Contracts.Halal.Dto;

/// <summary>
/// Possible statuses for a halal certificate lifecycle.
/// </summary>
public enum CertificateStatus
{
    Submitted,
    DocumentReview,
    Verification,
    Verified,
    ExpiringSoon,   // ≤ 90 days
    Expiring,       // ≤ 30 days
    Expired,
    Rejected,
    Revoked
}

/// <summary>
/// Possible compliance decisions produced by the Policy Engine.
/// AI agents collect evidence; this enum is the deterministic output.
/// </summary>
public enum ComplianceStatus
{
    Verified,
    ManualReview,
    Incomplete,
    Hold,
    NonCompliant,
    Unverified
}

public sealed record HalalCertificateDto(
    Guid Id,
    Guid ProductId,
    string CertificateNumber,
    string CertificationBody,
    string Jurisdiction,
    CertificateStatus Status,
    DateTimeOffset IssueDate,
    DateTimeOffset ExpiryDate,
    string? Scope,
    DateTimeOffset CreatedAt);

public sealed record VerificationDto(
    Guid Id,
    Guid ProductId,
    ComplianceStatus Status,
    string PolicyVersion,
    string Jurisdiction,
    string[] ReasonCodes,
    string[] MissingEvidence,
    bool RequiresHumanReview,
    DateTimeOffset VerifiedAt);

public sealed record VerificationEvidenceDto(
    Guid Id,
    Guid VerificationId,
    string EvidenceType,
    string Source,
    double Confidence,
    DateTimeOffset CollectedAt);

public sealed record VerificationAuditDto(
    Guid Id,
    Guid VerificationId,
    string Action,
    string Actor,
    string? Notes,
    DateTimeOffset OccurredAt);
