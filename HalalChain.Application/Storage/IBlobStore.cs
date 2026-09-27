namespace HalalChain.Application.Storage;

/// <summary>
/// Mechanism. Content-addressed put/get/exists. Deliberately has no Delete
/// member — append-only is a compile-time property, not a code-review norm.
/// </summary>
public interface IBlobStore
{
    Task<BlobRef> PutAsync(
        Stream content,
        BlobMetadata meta,
        CancellationToken ct = default);

    Task<Stream?> OpenReadAsync(
        BlobRef reference,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        BlobRef reference,
        CancellationToken ct = default);
}
