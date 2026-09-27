namespace HalalChain.Application.Storage;

/// <summary>
/// Domain concept. Composes IBlobStore with immutable ingest, audited reads,
/// and retention. This is what modules depend on, not IBlobStore directly.
/// </summary>
public interface IEvidenceStore
{
    Task<EvidenceRecord> IngestAsync(
        Stream content,
        EvidenceDescriptor descriptor,
        CancellationToken ct = default);

    Task<EvidenceRead> OpenAsync(
        EvidenceId id,
        string actorId,
        CancellationToken ct = default);

    Task<EvidenceRecord?> GetRecordAsync(
        EvidenceId id,
        CancellationToken ct = default);

    Task<RetentionReport> ApplyRetentionAsync(
        RetentionPolicy policy,
        CancellationToken ct = default);
}
