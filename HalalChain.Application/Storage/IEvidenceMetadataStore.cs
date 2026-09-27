namespace HalalChain.Application.Storage;

/// <summary>
/// Persistence for EvidenceRecord metadata. The API implements this against
/// EF Core/Postgres. A JSON-file adapter exists for dev and test.
/// </summary>
public interface IEvidenceMetadataStore
{
    Task InsertAsync(EvidenceRecord record, CancellationToken ct = default);
    Task<EvidenceRecord?> GetAsync(EvidenceId id, CancellationToken ct = default);
    Task<IReadOnlyList<EvidenceRecord>> QueryAsync(
        RetentionPolicy policy,
        int limit,
        CancellationToken ct = default);
    Task MarkTombstonedAsync(EvidenceId id, CancellationToken ct = default);
    Task ExtendLeaseAsync(EvidenceId id, TimeSpan extension, CancellationToken ct = default);
}
