namespace HalalChain.Agents.Traces;

using System.Text.Json;

using HalalChain.Application.Storage;

using Microsoft.Extensions.Logging;

/// <summary>
/// In-memory <see cref="IAgentTraceStore"/>, backed by the trace index the
/// agents service publishes.
///
/// Read-only by construction: there is no write or delete member, because the
/// comment on <see cref="IAgentTraceStore"/> is explicit that only the agents
/// service writes traces, through its own credential. Giving Application a
/// writer here would hand the managed layers the ability to author the
/// provenance records a verdict cites — which is the same class of hole as a
/// boolean on Product.
///
/// This is the read-side implementation over the trace index. The index itself
/// is populated out of band by the agents service.
/// </summary>
public sealed class InMemoryAgentTraceIndex : IAgentTraceStore
{
    private readonly Dictionary<TraceId, AgentTraceRecord> _traces = [];
    private readonly Dictionary<WorkflowRunId, List<TraceId>> _byRun = [];
    private readonly Dictionary<EvidenceId, List<TraceId>> _byEvidence = [];
    private readonly Lock _gate = new();

    public InMemoryAgentTraceIndex(IEnumerable<AgentTraceRecord>? seed = null)
    {
        if (seed is null)
            return;

        foreach (var record in seed)
            Index(record);
    }

    public Task<AgentTraceRecord?> GetAsync(TraceId id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_gate)
            return Task.FromResult(_traces.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<AgentTraceRecord>> QueryByWorkflowAsync(
        WorkflowRunId runId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_byRun.TryGetValue(runId, out var ids))
                return Task.FromResult<IReadOnlyList<AgentTraceRecord>>(Array.Empty<AgentTraceRecord>());

            IReadOnlyList<AgentTraceRecord> results = ids
                .Select(id => _traces[id])
                .OrderByDescending(r => r.CreatedAt)
                .ToArray();

            return Task.FromResult(results);
        }
    }

    public Task<IReadOnlyList<AgentTraceRecord>> QueryByEvidenceAsync(
        EvidenceId evidenceId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_byEvidence.TryGetValue(evidenceId, out var ids))
                return Task.FromResult<IReadOnlyList<AgentTraceRecord>>(Array.Empty<AgentTraceRecord>());

            IReadOnlyList<AgentTraceRecord> results = ids
                .Select(id => _traces[id])
                .OrderByDescending(r => r.CreatedAt)
                .ToArray();

            return Task.FromResult(results);
        }
    }

    /// <summary>
    /// Adds a record to the index. Called only at composition time from
    /// records the agents service has already published — not from request
    /// handling, and never with data this service manufactured.
    /// </summary>
    private void Index(AgentTraceRecord record)
    {
        _traces[record.Id] = record;

        if (!_byRun.TryGetValue(record.WorkflowRun, out var runIds))
            _byRun[record.WorkflowRun] = runIds = [];
        runIds.Add(record.Id);

        foreach (var evidenceId in record.CitedEvidence)
        {
            if (!_byEvidence.TryGetValue(evidenceId, out var evidenceIds))
                _byEvidence[evidenceId] = evidenceIds = [];
            evidenceIds.Add(record.Id);
        }
    }
}

/// <summary>
/// Resolves a trace by its id — index lookup, then content-addressed read
/// through the blob store.
///
/// Separate from <see cref="Eval.Traces.ITraceLoader"/> because the two have
/// different inputs and different failure modes. This one is how a verifier
/// answers "what reasoning produced this evidence?": it starts from a
/// <see cref="TraceId"/>, finds the index entry, and reads the body. Neither
/// step opens a file path, so the read-only credential boundary holds end to
/// end.
/// </summary>
public interface ITraceResolver
{
    Task<Eval.Dag.AgentTrace?> LoadByIdAsync(TraceId id, CancellationToken ct = default);

    Task<IReadOnlyList<Eval.Dag.AgentTrace>> LoadForWorkflowAsync(
        WorkflowRunId runId, CancellationToken ct = default);
}

public sealed class IndexedTraceResolver(
    IAgentTraceStore index,
    Eval.Traces.ITraceLoader loader,
    ILogger<IndexedTraceResolver> logger) : ITraceResolver
{
    public async Task<Eval.Dag.AgentTrace?> LoadByIdAsync(TraceId id, CancellationToken ct = default)
    {
        var record = await index.GetAsync(id, ct);
        if (record is null)
        {
            logger.LogWarning("Trace {TraceId} was not found in the trace index.", id);
            return null;
        }

        return await loader.LoadAsync(record.TraceBlob, ct);
    }

    public async Task<IReadOnlyList<Eval.Dag.AgentTrace>> LoadForWorkflowAsync(
        WorkflowRunId runId, CancellationToken ct = default)
    {
        var records = await index.QueryByWorkflowAsync(runId, ct);
        var traces = new List<Eval.Dag.AgentTrace>(records.Count);

        foreach (var record in records)
        {
            ct.ThrowIfCancellationRequested();
            var trace = await loader.LoadAsync(record.TraceBlob, ct);
            if (trace is not null)
                traces.Add(trace);
        }

        return traces;
    }
}
