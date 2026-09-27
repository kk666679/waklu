namespace HalalChain.Application.Storage;

public sealed record AccessRecord(
    AccessKind Kind,
    EvidenceId? EvidenceId,
    BlobRef? Blob,
    string ActorId,
    DateTimeOffset Timestamp,
    string? Detail);

public enum AccessKind
{
    Ingest,
    Read,
    Retention,
}
