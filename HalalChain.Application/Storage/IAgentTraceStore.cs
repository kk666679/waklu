namespace HalalChain.Application.Storage;

public interface IAgentTraceStore
{
    Task<AgentTraceRecord?> GetAsync(TraceId id, CancellationToken ct = default);

    Task<IReadOnlyList<AgentTraceRecord>> QueryByWorkflowAsync(
        WorkflowRunId runId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgentTraceRecord>> QueryByEvidenceAsync(
        EvidenceId evidenceId,
        CancellationToken ct = default);
}
