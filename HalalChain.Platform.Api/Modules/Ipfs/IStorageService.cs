namespace HalalChain.Platform.Api.Modules.Ipfs;

/// <summary>
/// Provider-agnostic content-addressable storage interface. Default
/// implementation is <see cref="PinataStorageService"/>; the local
/// kubo node is used for development via <see cref="LocalKuboStorageService"/>.
///
/// The contract is small on purpose: anything we put on IPFS is public
/// by default (content-addressed, gateway-fetchable). For sensitive
/// payloads, callers MUST use <see cref="StorageEncryption"/> to
/// AES-256-GCM-encrypt before calling <see cref="UploadAsync"/>.
/// </summary>
public interface IStorageService
{
    /// <summary>Upload a document. Returns the CID, size, and provider name.</summary>
    Task<PinResult> UploadAsync(UploadRequest req, CancellationToken ct);

    /// <summary>Download the raw bytes for a CID. Throws on CID mismatch.</summary>
    Task<Stream> DownloadAsync(string cid, CancellationToken ct);

    /// <summary>Check if a CID is currently pinned by this provider.</summary>
    Task<bool> IsPinnedAsync(string cid, CancellationToken ct);

    /// <summary>Pin an existing CID (e.g., uploaded by another node).</summary>
    Task PinAsync(string cid, CancellationToken ct);

    /// <summary>Unpin a CID. Caller is responsible for ensuring no consumers still need it.</summary>
    Task UnpinAsync(string cid, CancellationToken ct);

    /// <summary>Get provider-side metadata for a CID (size, content-type, pin time).</summary>
    Task<CidMetadata?> GetMetadataAsync(string cid, CancellationToken ct);

    /// <summary>Recompute the CID from the bytes and compare. Catches tampered/relayed gateway responses.</summary>
    Task<bool> VerifyAsync(string cid, byte[] expectedBytes, CancellationToken ct);

    /// <summary>Provider name for diagnostics ("pinata", "kubo-local", etc.).</summary>
    string ProviderName { get; }
}

public sealed record PinResult(string Cid, long SizeBytes, string Provider);

public sealed record CidMetadata(string Cid, long SizeBytes, string ContentType, DateTimeOffset PinnedAt);

public sealed record UploadRequest(
    string Name,
    Stream Content,
    string ContentType,
    StorageEncryption? Encryption = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record StorageOptions(
    string? PinName = null,
    long? PinTimeoutSeconds = null,
    bool WrapInDirectory = false);
