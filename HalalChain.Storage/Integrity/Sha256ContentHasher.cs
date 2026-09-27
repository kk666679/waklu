using System.Security.Cryptography;
using HalalChain.Application.Storage;

namespace HalalChain.Storage.Integrity;

public sealed class Sha256ContentHasher : IContentHasher
{
    public async Task<BlobRef> ComputeAsync(Stream content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var hash = await SHA256.HashDataAsync(content, ct).ConfigureAwait(false);
        return BlobRef.Create(Convert.ToHexString(hash).ToLowerInvariant());
    }
}
