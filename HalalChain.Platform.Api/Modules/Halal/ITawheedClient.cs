namespace HalalChain.Platform.Api.Modules.Halal;

/// <summary>
/// Client for the Tawheed multi-agent halal verification system.
/// AI agents collect evidence; the Policy Engine makes deterministic compliance decisions.
/// </summary>
public interface ITawheedClient
{
    Task<TawheedVerificationResult> RequestVerificationAsync(
        TawheedVerificationRequest request,
        CancellationToken ct = default);
}

public sealed class TawheedVerificationRequest
{
    public Guid ProductId { get; init; }
    public string ProductTitle { get; init; } = "";
    public string? Description { get; init; }
    public string[] Ingredients { get; init; } = [];
    public string CertificateNumber { get; init; } = "";
    public string CertificationBody { get; init; } = "";
    public string Jurisdiction { get; init; } = "MY";
    public string PolicyVersion { get; init; } = "MY-v3";
}

public sealed class TawheedVerificationResult
{
    public bool Success { get; init; }
    public string Status { get; init; } = "Pending"; // Pending, InProgress, Completed, RequiresReview, Rejected, Failed
    public string? ComplianceStatus { get; init; }
    public double RiskScore { get; init; }
    public string[] ReasonCodes { get; init; } = [];
    public string[] MissingEvidence { get; init; } = [];
    public bool RequiresHumanReview { get; init; }
    public TawheedAgentResult[] AgentResults { get; init; } = [];
    public string? ErrorMessage { get; init; }
}

public sealed class TawheedAgentResult
{
    public string AgentType { get; init; } = ""; // certificate, ingredient, supplier, document
    public string Status { get; init; } = ""; // completed, pending, failed
    public double Confidence { get; init; }
    public string[] EvidenceTypes { get; init; } = [];
    public string? Summary { get; init; }
}
