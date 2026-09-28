namespace HalalChain.Agents.Eval.Traces;

using System.Text.Json;
using System.Text.Json.Serialization;

using HalalChain.Agents.Eval.Dag;
using HalalChain.Application.Storage;

/// <summary>
/// Loads agent execution traces for evaluation.
///
/// Reads exclusively through <see cref="IBlobStore"/>. The eval harness holds
/// a read-only credential against evidence storage — the same credential any
/// auditor gets — so it must not be able to reach the filesystem directly or
/// open a path the blob store would not serve. Enforced by
/// EvaluationArchitectureTests.EvalReadsBlobOnlyThrough_IBlobStore.
///
/// It also has no Delete: evaluation reads and scores traces, it does not
/// destroy them. An eval run that could remove its own inputs is an eval run
/// that can launder a regression.
/// </summary>
public interface ITraceLoader
{
    Task<AgentTrace?> LoadAsync(BlobRef reference, CancellationToken ct = default);

    Task<IReadOnlyList<AgentTrace>> LoadManyAsync(
        IReadOnlyCollection<BlobRef> references, CancellationToken ct = default);
}

public sealed class BlobStoreTraceLoader(IBlobStore blobs) : ITraceLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<AgentTrace?> LoadAsync(BlobRef reference, CancellationToken ct = default)
    {
        await using var stream = await blobs.OpenReadAsync(reference, ct);
        if (stream is null)
            return null;

        return await JsonSerializer.DeserializeAsync<AgentTrace>(stream, JsonOptions, ct);
    }

    public async Task<IReadOnlyList<AgentTrace>> LoadManyAsync(
        IReadOnlyCollection<BlobRef> references, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(references);

        var traces = new List<AgentTrace>(references.Count);
        foreach (var reference in references)
        {
            ct.ThrowIfCancellationRequested();
            var trace = await LoadAsync(reference, ct);
            if (trace is not null)
                traces.Add(trace);
        }

        return traces;
    }
}

/// <summary>
/// Resolves a trace blob reference from the trace index.
///
/// Split from <see cref="ITraceLoader"/> because resolving and reading have
/// different failure modes: a missing index entry is a gap in the index, while
/// a blob that will not open is a storage problem. Callers report those
/// differently.
/// </summary>
public interface IAgentTraceIndex
{
    Task<AgentTraceRecord?> FindAsync(TraceId id, CancellationToken ct = default);
}
