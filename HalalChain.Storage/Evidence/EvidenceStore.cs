using HalalChain.Application.Storage;

namespace HalalChain.Storage.Evidence;

public sealed class EvidenceStore : IEvidenceStore
{
    /// <summary>
    /// Ingest buffers the stream once so it can hash and then put without
    /// requiring a rewind-capable stream from the caller. Evidence sizes are
    /// bounded by policy (certificates and lab reports, typically well under
    /// 100 MB), so buffering is acceptable and keeps the hash deterministic.
    /// Revisit before the first large-binary upload — see HalalChain.Storage/README.md.
    /// </summary>
    private static readonly TimeSpan SignedUrlTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ExtensionWindow = TimeSpan.FromDays(90);

    private const string ContentTypeTag = "content-type";
    private const string DefaultContentType = "application/octet-stream";
    private const string RetentionActor = "system:retention";

    private readonly IBlobStore _blobs;
    private readonly IEvidenceMetadataStore _metadata;
    private readonly IContentHasher _hasher;
    private readonly IAccessLog _accessLog;
    private readonly ISignedUrlIssuer _signedUrls;
    private readonly RetentionEvaluator _retention;

    public EvidenceStore(
        IBlobStore blobs,
        IEvidenceMetadataStore metadata,
        IContentHasher hasher,
        IAccessLog accessLog,
        ISignedUrlIssuer signedUrls,
        RetentionEvaluator retention)
    {
        _blobs = blobs;
        _metadata = metadata;
        _hasher = hasher;
        _accessLog = accessLog;
        _signedUrls = signedUrls;
        _retention = retention;
    }

    public async Task<EvidenceRecord> IngestAsync(
        Stream content,
        EvidenceDescriptor descriptor,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(descriptor);

        // Buffer once so we can hash, then put. Evidence sizes are bounded
        // by policy (typically <100MB), so buffering is acceptable and keeps
        // the hash deterministic without a rewind-capable stream contract.
        await using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, ct).ConfigureAwait(false);
        var size = buffered.Length;
        buffered.Position = 0;

        var blobRef = await _hasher.ComputeAsync(buffered, ct).ConfigureAwait(false);
        buffered.Position = 0;

        if (!await _blobs.ExistsAsync(blobRef, ct).ConfigureAwait(false))
        {
            var meta = BlobMetadata.Create(
                contentType: descriptor.Tags.TryGetValue(ContentTypeTag, out var declared)
                    ? declared : DefaultContentType,
                sizeBytes: size,
                originalFilename: descriptor.OriginalFilename,
                createdAt: DateTimeOffset.UtcNow,
                tags: descriptor.Tags);

            await _blobs.PutAsync(buffered, meta, ct).ConfigureAwait(false);
        }

        var record = new EvidenceRecord(
            Id: EvidenceId.New(),
            Blob: blobRef,
            Descriptor: descriptor,
            IngestedAt: DateTimeOffset.UtcNow);

        await _metadata.InsertAsync(record, ct).ConfigureAwait(false);

        await _accessLog.RecordAsync(new AccessRecord(
            Kind: AccessKind.Ingest,
            EvidenceId: record.Id,
            Blob: blobRef,
            ActorId: descriptor.ActorId,
            Timestamp: DateTimeOffset.UtcNow,
            Detail: descriptor.Kind.ToString()), ct).ConfigureAwait(false);

        return record;
    }

    public async Task<EvidenceRead> OpenAsync(
        EvidenceId id,
        string actorId,
        CancellationToken ct = default)
    {
        var record = await _metadata.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Evidence {id} not found.");

        // Log before issuing the URL — if the log write fails, no URL leaks.
        await _accessLog.RecordAsync(new AccessRecord(
            Kind: AccessKind.Read,
            EvidenceId: record.Id,
            Blob: record.Blob,
            ActorId: actorId,
            Timestamp: DateTimeOffset.UtcNow,
            Detail: null), ct).ConfigureAwait(false);

        if (!await _blobs.ExistsAsync(record.Blob, ct).ConfigureAwait(false))
            throw new InvalidOperationException(
                $"Evidence {id} metadata exists but blob {record.Blob} is missing.");

        var uri = await _signedUrls
            .IssueAsync(record.Blob, SignedUrlTtl, ct)
            .ConfigureAwait(false);

        return new EvidenceRead(
            SignedUri: uri,
            ExpiresAt: DateTimeOffset.UtcNow.Add(SignedUrlTtl),
            Record: record);
    }

    public Task<EvidenceRecord?> GetRecordAsync(EvidenceId id, CancellationToken ct = default)
        => _metadata.GetAsync(id, ct);

    public async Task<RetentionReport> ApplyRetentionAsync(
        RetentionPolicy policy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var candidates = await _metadata
            .QueryAsync(policy, limit: 1000, ct)
            .ConfigureAwait(false);

        int kept = 0, extended = 0, tombstoned = 0;

        foreach (var record in candidates)
        {
            var decision = _retention.Evaluate(record, policy);
            switch (decision)
            {
                case RetentionDecision.Keep:
                    kept++;
                    break;

                case RetentionDecision.Extend:
                    await _metadata.ExtendLeaseAsync(record.Id, ExtensionWindow, ct)
                        .ConfigureAwait(false);
                    extended++;
                    break;

                case RetentionDecision.Tombstone:
                    await _metadata.MarkTombstonedAsync(record.Id, ct).ConfigureAwait(false);
                    tombstoned++;

                    await _accessLog.RecordAsync(new AccessRecord(
                        Kind: AccessKind.Retention,
                        EvidenceId: record.Id,
                        Blob: record.Blob,
                        ActorId: RetentionActor,
                        Timestamp: DateTimeOffset.UtcNow,
                        Detail: "tombstoned"), ct).ConfigureAwait(false);
                    break;
            }
        }

        return new RetentionReport(
            Evaluated: candidates.Count,
            Kept: kept,
            Extended: extended,
            Tombstoned: tombstoned,
            CompletedAt: DateTimeOffset.UtcNow);
    }
}
