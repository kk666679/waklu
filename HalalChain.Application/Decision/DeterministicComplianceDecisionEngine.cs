using HalalChain.Application.Decision.Models;

namespace HalalChain.Application.Decision;

public interface IComplianceDecisionEngine
{
    DecisionResult Decide(IEnumerable<EvidenceItem> evidence, DecisionContext context);
}

public interface IEvidenceCollector
{
    IEnumerable<EvidenceItem> CollectEvidence(object input);
}

public sealed class DeterministicComplianceDecisionEngine : IComplianceDecisionEngine
{
    private readonly DecisionThresholdOptions _thresholds;

    public DeterministicComplianceDecisionEngine(DecisionThresholdOptions thresholds)
    {
        _thresholds = thresholds;
    }

    public DecisionResult Decide(IEnumerable<EvidenceItem> evidence, DecisionContext context)
    {
        var items = (evidence ?? Enumerable.Empty<EvidenceItem>()).ToArray();
        var validItems = items.Where(i => i.Confidence >= 0 && !string.IsNullOrWhiteSpace(i.Type)).ToArray();

        var score = validItems.Length == 0
            ? 0d
            : validItems.Average(i => i.Confidence);

        var outcome = score >= _thresholds.AutonomousThreshold && validItems.Length >= _thresholds.MinimumEvidence
            ? DecisionOutcome.AUTONOMOUS
            : score >= _thresholds.AssistedThreshold && validItems.Length >= _thresholds.MinimumEvidence
                ? DecisionOutcome.ASSISTED
                : DecisionOutcome.ESCALATED;

        var reasonCodes = context.ReasonCodes.Length > 0
            ? context.ReasonCodes
            : new[]
            {
                outcome == DecisionOutcome.AUTONOMOUS ? "AUTO_CONFIDENCE_OK" :
                outcome == DecisionOutcome.ASSISTED ? "HUMAN_ASSIST_REQUIRED" : "ESCALATE_FOR_REVIEW"
            };

        var auditTrail = new List<AuditEvent>
        {
            new("DECISION_EMITTED", "decision-engine", $"Outcome={outcome}; score={score:F3}", DateTimeOffset.UtcNow, "policy-engine"),
            new("THRESHOLDS_EVALUATED", "decision-engine", $"autonomous={_thresholds.AutonomousThreshold}; assisted={_thresholds.AssistedThreshold}; escalated={_thresholds.EscalatedThreshold}", DateTimeOffset.UtcNow, "policy-engine")
        };

        return new DecisionResult(
            outcome,
            score,
            reasonCodes,
            _thresholds,
            auditTrail,
            context.PolicyVersion,
            context.Jurisdiction);
    }
}
