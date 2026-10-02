namespace HalalChain.Automation.Browser;

using HalalChain.Application.Storage;

/// <summary>
/// Everything a browser execution is allowed to know.
///
/// Note what is absent: no HTTP context, no ambient user, no way to invent a
/// tenant. Tenant and actor are supplied by the caller and are authoritative,
/// which is what makes ADR-011 D4 ("no tenant ⇒ no session") enforceable —
/// a workflow JSON cannot smuggle a different tenant id, because it never
/// provides one (§37).
/// </summary>
public sealed record AutomationExecutionContext
{
    /// <summary>Unique id for this execution. Correlates logs, telemetry, evidence.</summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>The workflow this execution belongs to.</summary>
    public required string WorkflowId { get; init; }

    public required int WorkflowVersion { get; init; }

    /// <summary>Server-assigned tenant. Never read from workflow JSON.</summary>
    public required string TenantId { get; init; }

    /// <summary>Who asked for this. Used as the evidence actor id.</summary>
    public required string ActorId { get; init; }

    /// <summary>Correlates this run with the job that started it.</summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    /// <summary>Engine the workflow asked for. May be <see cref="BrowserEngineType.Default"/>.</summary>
    public BrowserEngineType Engine { get; init; } = BrowserEngineType.Default;

    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Scratch space threaded through nodes. Values are strings and evidence
    /// ids — no credentials, ever: the key space is enumerable by the designer.
    /// </summary>
    public IDictionary<string, object?> Variables { get; init; } =
        new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>Evidence ids produced so far, in capture order.</summary>
    public IList<EvidenceId> EvidenceIds { get; init; } = [];
}
