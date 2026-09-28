namespace HalalChain.Application.Agentic.Models;

public sealed record Escalation(
    EscalationReason Reason,
    string Detail,
    string ProposedAction);

public enum EscalationReason
{
    LowConfidence,
    NovelCase,
    ConflictingEvidence,
    PolicyAmbiguous,
}
