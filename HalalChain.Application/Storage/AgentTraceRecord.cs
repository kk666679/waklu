namespace HalalChain.Application.Storage;

/// <summary>
/// Provenance record for one agent step. The trace itself is content-addressed
/// in the blob store; this indexes it so a verdict can cite the exact reasoning
/// that produced its evidence.
///
/// Read-only from Application's perspective. Only the agents service writes
/// traces, through its own read-only-with-write-exception credential.
/// </summary>
public sealed record AgentTraceRecord(
    TraceId Id,
    WorkflowRunId WorkflowRun,
    string AgentName,
    string AgentVersion,
    BlobRef TraceBlob,
    IReadOnlyList<EvidenceId> CitedEvidence,
    string? ModelId,
    string? PromptVersion,
    double? Confidence,
    DateTimeOffset CreatedAt);
