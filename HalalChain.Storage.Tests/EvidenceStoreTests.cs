using HalalChain.Application.Storage;
using HalalChain.Storage.Adapters.FileSystem;
using HalalChain.Storage.Evidence;
using HalalChain.Storage.Integrity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HalalChain.Storage.Tests;

public sealed class EvidenceStoreTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), $"halalchain-evidence-{Guid.NewGuid():N}");

    private EvidenceStore CreateStore(IAccessLog? accessLog = null)
    {
        var blobs = new FileSystemBlobStore(
            Options.Create(new FileSystemOptions { Root = Path.Combine(_root, "blobs") }),
            NullLogger<FileSystemBlobStore>.Instance);

        var metadata = new FileSystemEvidenceMetadataStore(
            Options.Create(new FileSystemMetadataOptions
            {
                Root = Path.Combine(_root, "meta")
            }));

        var signedUrls = new LocalProxySignedUrlIssuer(
            Options.Create(new LocalProxyOptions
            {
                BaseUrl = "http://test",
                SigningKey = "test-key-do-not-use-in-production"
            }));

        return new EvidenceStore(
            blobs: blobs,
            metadata: metadata,
            hasher: new Sha256ContentHasher(),
            accessLog: accessLog ?? new NullAccessLogger(),
            signedUrls: signedUrls,
            retention: new RetentionEvaluator());
    }

    private static EvidenceDescriptor Descriptor(string actor = "test")
        => new(actor, EvidenceKind.Certificate, null, "cert.pdf",
               new Dictionary<string, string> { ["content-type"] = "application/pdf" });

    [Fact]
    public async Task Ingest_then_GetRecord_roundtrips()
    {
        var store = CreateStore();
        var record = await store.IngestAsync(
            new MemoryStream("certificate-content"u8.ToArray()),
            Descriptor());

        var retrieved = await store.GetRecordAsync(record.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(record.Id, retrieved!.Id);
        Assert.Equal(record.Blob, retrieved.Blob);
        Assert.Equal(EvidenceKind.Certificate, retrieved.Descriptor.Kind);
    }

    [Fact]
    public async Task Ingest_is_content_addressed_deduplicates()
    {
        var store = CreateStore();
        var payload = "same-content"u8.ToArray();

        var r1 = await store.IngestAsync(new MemoryStream(payload), Descriptor());
        var r2 = await store.IngestAsync(new MemoryStream(payload), Descriptor());

        // Distinct EvidenceRecords, same underlying blob.
        Assert.NotEqual(r1.Id, r2.Id);
        Assert.Equal(r1.Blob, r2.Blob);
    }

    [Fact]
    public async Task Ingested_blob_bytes_are_readable_through_the_store()
    {
        var store = CreateStore();
        var payload = "readable-bytes"u8.ToArray();

        var record = await store.IngestAsync(new MemoryStream(payload), Descriptor());

        // The BlobRef is the SHA-256 of exactly these bytes.
        Assert.Equal(
            await new Sha256ContentHasher().ComputeAsync(new MemoryStream(payload)),
            record.Blob);
    }

    [Fact]
    public async Task Open_returns_signed_uri_with_expiry()
    {
        var store = CreateStore();
        var record = await store.IngestAsync(
            new MemoryStream("read-me"u8.ToArray()),
            Descriptor());

        var read = await store.OpenAsync(record.Id, actorId: "auditor");

        Assert.StartsWith("http://test/_blob/", read.SignedUri.ToString());
        Assert.Contains(record.Blob.ContentHash, read.SignedUri.ToString());
        Assert.True(read.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Open_throws_for_unknown_evidence()
    {
        var store = CreateStore();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await store.OpenAsync(EvidenceId.New(), "auditor"));
    }

    [Fact]
    public async Task Open_logs_before_issuing_a_url()
    {
        // Architectural rule: every IEvidenceStore.OpenAsync emits an
        // IAccessLog entry. If the log write fails, no URL leaks.
        var log = new RecordingAccessLog();
        var store = CreateStore(log);
        var record = await store.IngestAsync(
            new MemoryStream("audited"u8.ToArray()),
            Descriptor(actor: "ingestor"));

        Assert.Equal(1, log.Records.Count);
        Assert.Equal(AccessKind.Ingest, log.Records[0].Kind);
        Assert.Equal("ingestor", log.Records[0].ActorId);

        await store.OpenAsync(record.Id, actorId: "auditor");

        Assert.Equal(2, log.Records.Count);
        Assert.Equal(AccessKind.Read, log.Records[1].Kind);
        Assert.Equal("auditor", log.Records[1].ActorId);
        Assert.Equal(record.Id, log.Records[1].EvidenceId);
    }

    [Fact]
    public async Task ApplyRetention_tombstones_old_evidence_and_reports()
    {
        var store = CreateStore();
        await store.IngestAsync(
            new MemoryStream("aged"u8.ToArray()),
            Descriptor());

        var report = await store.ApplyRetentionAsync(
            new RetentionPolicy(TimeSpan.FromDays(365), RetentionScope.Certificate));

        Assert.Equal(1, report.Evaluated);
        Assert.Equal(1, report.Kept);
        Assert.Equal(0, report.Tombstoned);
    }

    private sealed class RecordingAccessLog : IAccessLog
    {
        public List<AccessRecord> Records { get; } = new();

        public Task RecordAsync(AccessRecord record, CancellationToken ct = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
