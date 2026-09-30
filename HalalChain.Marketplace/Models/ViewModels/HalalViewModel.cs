namespace HalalChain.Marketplace.Models.ViewModels;

/// <summary>ViewModel for displaying Halal Verification result.</summary>
public class HalalVerificationViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ComplianceStatus { get; set; } = string.Empty; // Verified, ManualReview, Incomplete, Hold, NonCompliant, Unverified
    public string PolicyVersion { get; set; } = "MY-v3";
    public string Jurisdiction { get; set; } = "MY";
    public bool RequiresHumanReview { get; set; }
    public string[] ReasonCodes { get; set; } = [];
    public string[] MissingEvidence { get; set; } = [];
    public DateTime VerifiedAt { get; set; }

    // Metrics for detail view
    public int EvidenceCompleteness { get; set; } = 0;
    public int ComplianceScore { get; set; } = 0;

    public string DisplayStatus => ComplianceStatus switch
    {
        "Verified" => "✓ Verified",
        "ManualReview" => "⟳ Under Review",
        "Incomplete" => "⊗ Incomplete",
        "Hold" => "⏸ On Hold",
        "NonCompliant" => "✗ Non-Compliant",
        _ => "? Unverified"
    };

    public string StatusBadgeClass => ComplianceStatus switch
    {
        "Verified" => "badge bg-success",
        "ManualReview" => "badge bg-info",
        "Incomplete" => "badge bg-warning",
        "Hold" => "badge bg-warning",
        "NonCompliant" => "badge bg-danger",
        _ => "badge bg-secondary"
    };
}

/// <summary>ViewModel for displaying a Certificate.</summary>
public class CertificateViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public string CertificationBody { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = "MY";
    public string Status { get; set; } = string.Empty; // Submitted, DocumentReview, Verification, Verified, ExpiringSoon, Expiring, Expired, Rejected, Revoked
    public string? Scope { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiryDate;
    public bool IsExpiringSoon => !IsExpired && DateTime.UtcNow.AddDays(90) > ExpiryDate;
    public int DaysUntilExpiry => (ExpiryDate.Date - DateTime.UtcNow.Date).Days;

    public string DisplayStatus => Status switch
    {
        "Verified" => "✓ Verified",
        "ExpiringSoon" => "⚠ Expiring Soon",
        "Expired" => "✗ Expired",
        "Rejected" => "✗ Rejected",
        _ => Status
    };

    public string StatusBadgeClass => Status switch
    {
        "Verified" => "badge bg-success",
        "DocumentReview" => "badge bg-info",
        "Verification" => "badge bg-primary",
        "ExpiringSoon" => "badge bg-warning",
        "Expired" => "badge bg-danger",
        "Rejected" => "badge bg-danger",
        _ => "badge bg-secondary"
    };
}

/// <summary>ViewModel for displaying verification evidence items.</summary>
public class VerificationEvidenceViewModel
{
    public Guid Id { get; set; }
    public Guid VerificationId { get; set; }
    public string EvidenceType { get; set; } = string.Empty; // Certificate, Documentation, Audit, Lab, Supplier, etc.
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Collected, Pending, Failed, Verified
    public string? Source { get; set; }
    public string? DocumentUrl { get; set; }
    public DateTime CollectedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>ViewModel for displaying verification audit trail events.</summary>
public class VerificationAuditViewModel
{
    public Guid Id { get; set; }
    public Guid VerificationId { get; set; }
    public string ActionName { get; set; } = string.Empty; // Created, Updated, Reviewed, Approved, Rejected, etc.
    public string? PerformedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
