namespace HalalChain.Domain.Halal;

/// <summary>
/// A halal certificate submitted by a vendor for a product.
/// Lifecycle: Submitted → DocumentReview → Verification → Verified/Rejected/Expired
/// </summary>
public sealed class Certificate
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string CertificationBody { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = "MY";
    public CertificateStatus Status { get; set; } = CertificateStatus.Submitted;
    public DateTimeOffset IssueDate { get; set; }
    public DateTimeOffset ExpiryDate { get; set; }
    public string? Scope { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<HalalVerification> Verifications { get; set; } = [];
}
