using System.Reflection;
using System.Text;

using HalalChain.Agents.Eval.Dag;
using HalalChain.Agents.Eval.Traces;
using HalalChain.Agents.Traces;
using HalalChain.Application.Storage;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace HalalChain.Agents.Tests;

public sealed class TraceLoadingTests
{
    private const string HexHash = "a3f1c2d4e5b60718293a4b5c6d7e8f90a1b2c3d4e5f60718293a4b5c6d7e8f90";

    private sealed class InMemoryBlobStore(Dictionary<BlobRef, byte[]> blobs) : IBlobStore
    {
        public int OpenReadCount { get; private set; }

        public Task<BlobRef> PutAsync(Stream content, BlobMetadata meta, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Stream?> OpenReadAsync(BlobRef reference, CancellationToken ct = default)
        {
            OpenReadCount++;
            return Task.FromResult<Stream?>(
                blobs.TryGetValue(reference, out var bytes) ? new MemoryStream(bytes) : null);
        }

        public Task<bool> ExistsAsync(BlobRef reference, CancellationToken ct = default) =>
            Task.FromResult(blobs.ContainsKey(reference));
    }

    private static AgentTraceRecord Record(TraceId id, WorkflowRunId run, BlobRef blob) => new(
        id, run, "collector", "1.0.0", blob, Array.Empty<EvidenceId>(), "claude", "v1", 0.9,
        DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task Loader_DeserializesATraceFromTheBlobStore()
    {
        var reference = BlobRef.Create(HexHash);
        var json = """
            {"traceId":"t1","workflowName":"supplier_onboarding","startedAt":"2026-01-01T00:00:00Z",
             "nodes":[{"id":"collector","type":"collector"}],"edges":[]}
            """;

        var store = new InMemoryBlobStore(new Dictionary<BlobRef, byte[]> { [reference] = Encoding.UTF8.GetBytes(json) });
        var loader = new BlobStoreTraceLoader(store);

        var trace = await loader.LoadAsync(reference, TestContext.Current.CancellationToken);

        Assert.NotNull(trace);
        Assert.Equal("t1", trace!.TraceId);
        Assert.Equal(WorkflowNodeType.Collector, Assert.Single(trace.Nodes).Type);
    }

    [Fact]
    public async Task Loader_ReturnsNullForAMissingBlob()
    {
        var store = new InMemoryBlobStore([]);
        var loader = new BlobStoreTraceLoader(store);

        Assert.Null(await loader.LoadAsync(BlobRef.Create(HexHash), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Loader_ReadsOnlyThroughTheBlobStore()
    {
        // The eval credential is read-only against evidence storage. If any
        // eval type could open a file directly, that boundary is decorative.
        var reference = BlobRef.Create(HexHash);
        var json = """{"traceId":"t1","workflowName":"w","startedAt":"2026-01-01T00:00:00Z","nodes":[],"edges":[]}""";
        var store = new InMemoryBlobStore(new Dictionary<BlobRef, byte[]> { [reference] = Encoding.UTF8.GetBytes(json) });

        await new BlobStoreTraceLoader(store).LoadAsync(reference, TestContext.Current.CancellationToken);

        Assert.Equal(1, store.OpenReadCount);
    }

    [Fact]
    public async Task Index_FindsATraceById()
    {
        var id = TraceId.New();
        var record = Record(id, WorkflowRunId.New(), BlobRef.Create(HexHash));
        var index = new InMemoryAgentTraceIndex([record]);

        var found = await index.GetAsync(id, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(id, found!.Id);
    }

    [Fact]
    public async Task Index_QueriesByWorkflowRun()
    {
        var run = WorkflowRunId.New();
        var records = new[]
        {
            Record(TraceId.New(), run, BlobRef.Create(HexHash)),
            Record(TraceId.New(), WorkflowRunId.New(), BlobRef.Create(HexHash)),
        };

        var index = new InMemoryAgentTraceIndex(records);

        // Only the first record belongs to this run.
        var found = await index.QueryByWorkflowAsync(run, TestContext.Current.CancellationToken);

        Assert.Single(found);
        Assert.Equal(records[0].Id, found[0].Id);
    }

    [Fact]
    public async Task Index_QueriesByCitedEvidence()
    {
        var evidence = EvidenceId.New();
        var withEvidence = Record(TraceId.New(), WorkflowRunId.New(), BlobRef.Create(HexHash)) with
        {
            CitedEvidence = [evidence],
        };

        var index = new InMemoryAgentTraceIndex([withEvidence, Record(TraceId.New(), WorkflowRunId.New(), BlobRef.Create(HexHash))]);

        Assert.Single(await index.QueryByEvidenceAsync(evidence, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Index_DeclaresNoWriteOrDeleteMember()
    {
        // Only the agents service writes traces. A writer here would hand the
        // managed layers the ability to author the provenance a verdict cites.
        var members = typeof(InMemoryAgentTraceIndex)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .Where(n => n.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Remove", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Write", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Add", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Update", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(members.Count == 0, "Offenders:\n" + string.Join("\n", members));
    }

    [Fact]
    public async Task Resolver_ReturnsNullWhenTheTraceIsNotIndexed()
    {
        var store = new InMemoryBlobStore([]);
        var index = new InMemoryAgentTraceIndex();

        var resolver = new IndexedTraceResolver(
            index,
            new BlobStoreTraceLoader(store),
            NullLogger<IndexedTraceResolver>.Instance);

        Assert.Null(await resolver.LoadByIdAsync(TraceId.New(), TestContext.Current.CancellationToken));
    }
}
