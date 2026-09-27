namespace HalalChain.Application.Storage;

/// <summary>
/// SHA-256 only. This is the *storage* hasher: it addresses <see cref="BlobRef"/>.
/// Keccak256 for on-chain Merkle internal nodes is a different concern and lives
/// in the blockchain module (merge decision M1). Never reference a Merkle tree
/// from this namespace.
/// </summary>
public interface IContentHasher
{
    Task<BlobRef> ComputeAsync(Stream content, CancellationToken ct = default);
}
