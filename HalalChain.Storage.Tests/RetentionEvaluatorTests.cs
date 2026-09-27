using HalalChain.Application.Storage;
using HalalChain.Storage.Evidence;
using Xunit;

namespace HalalChain.Storage.Tests;

public sealed class RetentionEvaluatorTests
{
    private readonly RetentionEvaluator _evaluator = new();

    private static EvidenceRecord Record(EvidenceKind kind, TimeSpan age)
        => new(
            EvidenceId.New(),
            BlobRef.Create(new string('a', 64)),
            new EvidenceDescriptor(
                "test", kind, null, null,
                new Dictionary<string, string>()),
            DateTimeOffset.UtcNow - age);

    [Fact]
    public void Fresh_certificate_is_kept()
    {
        var decision = _evaluator.Evaluate(
            Record(EvidenceKind.Certificate, TimeSpan.FromDays(30)),
            new RetentionPolicy(TimeSpan.FromDays(365), RetentionScope.All));

        Assert.Equal(RetentionDecision.Keep, decision);
    }

    [Fact]
    public void Old_certificate_is_extended_then_tombstoned()
    {
        var policy = new RetentionPolicy(TimeSpan.FromDays(365), RetentionScope.Certificate);

        var just_past_window = Record(EvidenceKind.Certificate, TimeSpan.FromDays(400));
        Assert.Equal(RetentionDecision.Extend,
            _evaluator.Evaluate(just_past_window, policy));

        var well_past_window = Record(EvidenceKind.Certificate, TimeSpan.FromDays(500));
        Assert.Equal(RetentionDecision.Tombstone,
            _evaluator.Evaluate(well_past_window, policy));
    }

    [Fact]
    public void Scope_mismatch_keeps_record_untouched()
    {
        var decision = _evaluator.Evaluate(
            Record(EvidenceKind.Invoice, TimeSpan.FromDays(1000)),
            new RetentionPolicy(TimeSpan.FromDays(365), RetentionScope.Certificate));

        Assert.Equal(RetentionDecision.Keep, decision);
    }

    [Fact]
    public void Agent_traces_tombstone_earlier_than_certificates()
    {
        var policy = new RetentionPolicy(TimeSpan.FromDays(30), RetentionScope.All);

        var trace = Record(EvidenceKind.AgentTrace, TimeSpan.FromDays(90));
        Assert.Equal(RetentionDecision.Tombstone, _evaluator.Evaluate(trace, policy));

        var cert = Record(EvidenceKind.Certificate, TimeSpan.FromDays(90));
        Assert.Equal(RetentionDecision.Extend, _evaluator.Evaluate(cert, policy));
    }

    [Fact]
    public void Evaluation_is_deterministic()
    {
        var record = Record(EvidenceKind.Certificate, TimeSpan.FromDays(400));
        var policy = new RetentionPolicy(TimeSpan.FromDays(365), RetentionScope.All);

        var first = _evaluator.Evaluate(record, policy);
        for (int i = 0; i < 5; i++)
            Assert.Equal(first, _evaluator.Evaluate(record, policy));
    }
}
