namespace HalalChain.Application.Tawheed.Models;

public sealed record PolicyEvaluationResponse(
    Verdict Verdict,
    string PolicyVersion,
    IReadOnlyList<PolicyGap> Gaps,
    string? TraceHash,
    DateTimeOffset DecidedAt);

public enum Verdict
{
    Halal,
    NotHalal,
    InsufficientEvidence,
}

public sealed record PolicyGap(
    string RequirementId,
    string Description,
    string Severity);

public sealed record PolicyVersion(
    string Version,
    string ContentHash,
    DateTimeOffset EffectiveFrom);
