using System.ComponentModel.DataAnnotations;

namespace HalalChain.Platform.Contracts.Halal.Requests;

public sealed class SubmitCertificateRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Required, MinLength(1), MaxLength(100)]
    public string CertificateNumber { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(100)]
    public string CertificationBody { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(10)]
    public string Jurisdiction { get; init; } = string.Empty;

    [Required]
    public DateTimeOffset IssueDate { get; init; }

    [Required]
    public DateTimeOffset ExpiryDate { get; init; }

    [MaxLength(1000)]
    public string? Scope { get; init; }
}

public sealed class TriggerVerificationRequest
{
    [Required, MinLength(1), MaxLength(10)]
    public string Jurisdiction { get; init; } = "MY";

    [Required, MinLength(1), MaxLength(50)]
    public string PolicyVersion { get; init; } = "MY-v3";

    [MaxLength(5000)]
    public string? DocumentText { get; init; }
}

public sealed class RecordVerificationDecisionRequest
{
    [Required, MinLength(1), MaxLength(20)]
    public string Decision { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; init; }
}
