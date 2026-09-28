namespace HalalChain.Application.Agentic.Models;

/// <summary>
/// Output of any agent workflow step.
///
/// This type has no verdict field, and it must never gain one. Agents
/// gather and interpret evidence; they do not decide. Enforced by
/// HalalChain.Architecture.Tests and by a CI grep.
///
/// Deliberately absent, and must remain absent:
///   Verdict, Decision, IsHalal, HalalStatus, Approved, Rejected
/// </summary>
public sealed record EvidenceProposal
{
    public required EvidenceKind Kind { get; init; }
    public required string Summary { get; init; }
    public required double Confidence { get; init; }
    public required IReadOnlyList<SourceRef> SourceRefs { get; init; }
    public required IReadOnlyDictionary<string, object> ProposedEvidence { get; init; }
}
