namespace HalalChain.Domain.Blockchain;

/// <summary>On-chain mirror of a SupplierRegistry entry.</summary>
public sealed class SupplierOnChain
{
    public Guid Id { get; set; }
    public string SupplierId { get; set; } = string.Empty;
    public string Wallet { get; set; } = string.Empty;
    public string IpfsMetadataCid { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public long RegisteredAtUnix { get; set; }
    public long UpdatedAtUnix { get; set; }
    public int Status { get; set; }
    public string TxHash { get; set; } = string.Empty;
    public int BlockNumber { get; set; }
    public DateTimeOffset IndexedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ProductOnChain
{
    public Guid Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public string MetadataHashHex { get; set; } = string.Empty;
    public string IpfsCid { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public string CurrentCertId { get; set; } = string.Empty;
    public long RegisteredAtUnix { get; set; }
    public long UpdatedAtUnix { get; set; }
    public int Status { get; set; }
    public string TxHash { get; set; } = string.Empty;
    public int BlockNumber { get; set; }
    public DateTimeOffset IndexedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CertificateOnChain
{
    public Guid Id { get; set; }
    public string CertId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string Certifier { get; set; } = string.Empty;
    public string DocumentCid { get; set; } = string.Empty;
    public long IssuedAtUnix { get; set; }
    public long ExpiresAtUnix { get; set; }
    public string ScopeCid { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int Status { get; set; }
    public string TxHash { get; set; } = string.Empty;
    public int BlockNumber { get; set; }
    public DateTimeOffset IndexedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TraceabilityEventOnChain
{
    public Guid Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public int EventType { get; set; }
    public string LocationCid { get; set; } = string.Empty;
    public string EvidenceCid { get; set; } = string.Empty;
    public string NotesCid { get; set; } = string.Empty;
    public long TimestampUnix { get; set; }
    public string TxHash { get; set; } = string.Empty;
    public int BlockNumber { get; set; }
    public int LogIndex { get; set; }
    public DateTimeOffset IndexedAt { get; set; } = DateTimeOffset.UtcNow;
}
