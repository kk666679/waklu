using HalalChain.Application.Decision;
using HalalChain.Application.Decision.Models;
using FluentAssertions;

namespace HalalChain.Platform.Tests;

public sealed class TrustBoundaryTests
{
    [Fact]
    public void AiEvidenceAlone_CannotProduceAutonomousDecision()
    {
        var engine = new DeterministicComplianceDecisionEngine(new DecisionThresholdOptions
        {
            AutonomousThreshold = 0.85,
            AssistedThreshold = 0.65,
            EscalatedThreshold = 0.50,
            MinimumEvidence = 2
        });

        var evidence = new[]
        {
            new EvidenceItem("ingredient", 0.82, "supplier-1"),
            new EvidenceItem("document", 0.79, "document-1")
        };

        var result = engine.Decide(evidence, new DecisionContext { Jurisdiction = "MY", PolicyVersion = "MY-v3" });

        result.Outcome.Should().NotBe(DecisionOutcome.AUTONOMOUS);
    }

    [Fact]
    public void DecisionEngine_EmitsAuditRecordForEveryDecision()
    {
        var engine = new DeterministicComplianceDecisionEngine(new DecisionThresholdOptions
        {
            AutonomousThreshold = 0.85,
            AssistedThreshold = 0.65,
            EscalatedThreshold = 0.50,
            MinimumEvidence = 2
        });

        var evidence = new[]
        {
            new EvidenceItem("certificate", 0.91, "cert-1"),
            new EvidenceItem("ingredient", 0.88, "supplier-1"),
            new EvidenceItem("document", 0.84, "doc-1")
        };

        var result = engine.Decide(evidence, new DecisionContext { Jurisdiction = "MY", PolicyVersion = "MY-v3" });

        result.AuditTrail.Should().NotBeNullOrEmpty();
        result.AuditTrail.Should().Contain(a => a.Action == "DECISION_EMITTED");
    }

    [Fact]
    public void Thresholds_AreConfigDriven_NotHardcoded()
    {
        var highThresholds = new DecisionThresholdOptions
        {
            AutonomousThreshold = 0.99,
            AssistedThreshold = 0.80,
            EscalatedThreshold = 0.60,
            MinimumEvidence = 2
        };

        var engine = new DeterministicComplianceDecisionEngine(highThresholds);
        var evidence = new[]
        {
            new EvidenceItem("certificate", 0.92, "cert-1"),
            new EvidenceItem("ingredient", 0.90, "supplier-1"),
            new EvidenceItem("document", 0.88, "doc-1")
        };

        var result = engine.Decide(evidence, new DecisionContext { Jurisdiction = "MY", PolicyVersion = "MY-v3" });

        result.Outcome.Should().NotBe(DecisionOutcome.AUTONOMOUS);
        result.Thresholds.Should().BeSameAs(highThresholds);
    }
}
