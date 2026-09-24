namespace HalalChain.Application.Decision.Models;

public enum DecisionOutcome
{
    AUTONOMOUS,
    ASSISTED,
    ESCALATED
}

public sealed record DecisionThresholdOptions
{
    public double AutonomousThreshold { get; init; } = 0.85;
    public double AssistedThreshold { get; init; } = 0.65;
    public double EscalatedThreshold { get; init; } = 0.50;
    public int MinimumEvidence { get; init; } = 2;
}

public sealed record EvidenceItem(string Type, double Confidence, string Source);

public sealed record DecisionContext
{
    public string Jurisdiction { get; init; } = "MY";
    public string PolicyVersion { get; init; } = "MY-v3";
    public string[] ReasonCodes { get; init; } = [];
}

public sealed record AuditEvent(
    string Action,
    string Subject,
    string Details,
    DateTimeOffset Timestamp,
    string Signer);

public sealed record DecisionResult(
    DecisionOutcome Outcome,
    double Confidence,
    string[] ReasonCodes,
    DecisionThresholdOptions Thresholds,
    IReadOnlyList<AuditEvent> AuditTrail,
    string? PolicyVersion = null,
    string? Jurisdiction = null);
