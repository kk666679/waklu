using HalalChain.Application.Storage;

namespace HalalChain.Storage.Evidence;

/// <summary>
/// Deterministic. No LLM, no external calls. The same inputs always produce
/// the same decision — that's what makes an audit trail defensible.
/// </summary>
public sealed class RetentionEvaluator
{
    /// <summary>Grace window after the policy window before a record is extended.</summary>
    private static readonly TimeSpan ExtensionWindow = TimeSpan.FromDays(90);

    /// <summary>Agent traces lose their extended window 30 days after the policy window.</summary>
    private static readonly TimeSpan AgentTraceGrace = TimeSpan.FromDays(30);

    public RetentionDecision Evaluate(EvidenceRecord record, RetentionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(policy);

        if (policy.Scope != RetentionScope.All && !Matches(record, policy.Scope))
            return RetentionDecision.Keep;

        var age = DateTimeOffset.UtcNow - record.IngestedAt;
        if (age < policy.Window)
            return RetentionDecision.Keep;

        // Agent traces are tombstoned sooner than certificates — they are
        // provenance, not primary evidence, and PII risk is higher.
        if (record.Descriptor.Kind == EvidenceKind.AgentTrace
            && age > policy.Window + AgentTraceGrace)
        {
            return RetentionDecision.Tombstone;
        }

        if (age < policy.Window + ExtensionWindow)
            return RetentionDecision.Extend;

        return RetentionDecision.Tombstone;
    }

    private static bool Matches(EvidenceRecord record, RetentionScope scope)
        => scope switch
        {
            RetentionScope.Certificate => record.Descriptor.Kind == EvidenceKind.Certificate,
            RetentionScope.Audit       => record.Descriptor.Kind == EvidenceKind.SupplierAudit,
            RetentionScope.AgentTrace  => record.Descriptor.Kind == EvidenceKind.AgentTrace,
            _ => true,
        };
}
