namespace HalalChain.Platform.Api.Modules.Blockchain;

/// <summary>
/// On-chain mirror of a Supplier. Decoupled from the off-chain DB row
/// (which holds PII, addresses, phone, etc.). The supplierId is the
/// stable off-chain-issued identifier (e.g. SUP-MY-000123).
/// </summary>
public sealed record SupplierOnChainDto(
    string SupplierId,
    string Wallet,
    string IpfsMetadataCid,
    string Jurisdiction,
    DateTimeOffset RegisteredAt,
    DateTimeOffset UpdatedAt,
    int Status); // SupplierStatus enum: 0=Active, 1=Suspended, 2=Revoked

public sealed record ProductOnChainDto(
    string ProductId,
    string SupplierId,
    string MetadataHashHex,
    string IpfsCid,
    string Jurisdiction,
    string CurrentCertId,            // empty string = none
    DateTimeOffset RegisteredAt,
    DateTimeOffset UpdatedAt,
    int Status); // ProductStatus: 0=Pending, 1=Verified, 2=Suspended, 3=Recalled

public sealed record CertificateOnChainDto(
    string CertId,
    string ProductId,
    string Certifier,
    string DocumentCid,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string ScopeCid,
    string Country,
    int Status); // CertificateStatus: 0=Active, 1=Revoked, 2=Expired

public sealed record TraceabilityEventOnChainDto(
    string EventId,
    string ProductId,
    string BatchId,
    string Actor,
    int EventType, // EventType enum
    string LocationCid,
    string EvidenceCid,
    string NotesCid,
    DateTimeOffset Timestamp);

/// <summary>
/// Result of a write to the chain. The hash is the transaction hash.
/// The status progresses: Requested -> Pending -> Confirmed | Failed | Reverted.
/// </summary>
public sealed record ChainWriteResult(
    string TxHash,
    string Status,                     // "Requested" | "Pending" | "Confirmed" | "Failed" | "Reverted"
    int? BlockNumber,
    DateTimeOffset SubmittedAt,
    string? Error);

public sealed record ChainReadResult<T>(T Value, string Source); // Source = "chain" | "indexer" | "fallback"
