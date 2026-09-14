namespace HalalChain.Domain.Halal;

/// <summary>
/// Possible compliance decisions produced by the Policy Engine.
/// AI agents collect evidence; this enum is the deterministic output.
/// Numeric values are kept in sync with
/// <c>HalalChain.Platform.Contracts.Halal.Dto.ComplianceStatus</c>.
/// </summary>
public enum ComplianceStatus
{
    Verified = 0,
    ManualReview = 1,
    Incomplete = 2,
    Hold = 3,
    NonCompliant = 4,
    Unverified = 5
}

/// <summary>
/// Possible statuses for a halal certificate lifecycle.
/// Numeric values are kept in sync with
/// <c>HalalChain.Platform.Contracts.Halal.Dto.CertificateStatus</c>.
/// </summary>
public enum CertificateStatus
{
    Submitted = 0,
    DocumentReview = 1,
    Verification = 2,
    Verified = 3,
    ExpiringSoon = 4,
    Expiring = 5,
    Expired = 6,
    Rejected = 7,
    Revoked = 8
}
