namespace HalalChain.Platform.Api.Modules.Blockchain;

/// <summary>
/// Public façade for everything the HalalChain backend does on-chain.
/// Implemented by <see cref="SmartContractService"/> which wraps Nethereum
/// against the 5 deployed contracts (AccessControl, SupplierRegistry,
/// HalalProductRegistry, HalalCertificationRegistry, TraceabilityEventLog).
///
/// All write methods return a <see cref="ChainWriteResult"/> and are
/// durable via the outbox in <see cref="TransactionQueue"/> — a write
/// survives a process crash and is replayed on restart.
///
/// All read methods return a <see cref="ChainReadResult{T}"/> with a
/// source indicator so the caller can show "verified on-chain" vs
/// "from our indexer cache" in the UI.
/// </summary>
public interface ISmartContractService
{
    // ── Writes (require the platform operator wallet / a certifier) ──
    Task<ChainWriteResult> RegisterSupplierAsync(string supplierId, string wallet, string ipfsMetadataCid, string jurisdiction, CancellationToken ct);
    Task<ChainWriteResult> RegisterProductAsync(string productId, string supplierId, string metadataHashHex, string ipfsCid, string jurisdiction, CancellationToken ct);
    Task<ChainWriteResult> IssueCertificateAsync(string certId, string productId, string documentCid, DateTimeOffset expiresAt, string scopeCid, string country, CancellationToken ct);
    Task<ChainWriteResult> RevokeCertificateAsync(string certId, string reasonCid, CancellationToken ct);
    Task<ChainWriteResult> ExpireCertificateAsync(string certId, CancellationToken ct);
    Task<ChainWriteResult> RecordTraceabilityEventAsync(string eventId, string productId, string batchId, int eventType, string locationCid, string evidenceCid, string notesCid, CancellationToken ct);
    Task<ChainWriteResult> RecallProductAsync(string productId, string reasonCid, CancellationToken ct);

    // ── Reads (always direct from chain; for the public /verify page) ──
    Task<ChainReadResult<SupplierOnChainDto?>>      GetSupplierAsync(string supplierId, CancellationToken ct);
    Task<ChainReadResult<ProductOnChainDto?>>       GetProductAsync(string productId, CancellationToken ct);
    Task<ChainReadResult<CertificateOnChainDto?>>   GetCertificateAsync(string certId, CancellationToken ct);
    Task<ChainReadResult<CertificateOnChainDto?>>   GetCurrentCertificateForProductAsync(string productId, CancellationToken ct);
    Task<ChainReadResult<IReadOnlyList<TraceabilityEventOnChainDto>>> GetTraceabilityAsync(string productId, CancellationToken ct);

    // ── Health ──
    Task<ChainReadResult<ulong>> GetBlockNumberAsync(CancellationToken ct);
    Task<bool> IsHealthyAsync(CancellationToken ct);

    // ── Wallet role helpers (for the indexer to attribute events) ──
    string? WalletAddress { get; }
}
