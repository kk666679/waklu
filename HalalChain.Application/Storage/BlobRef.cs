using System.Text.Json.Serialization;

namespace HalalChain.Application.Storage;

/// <summary>
/// Content-addressed reference to a blob. SHA-256, lowercase hex, 64 chars.
/// Value equality is structural — two BlobRefs with the same hash are the same blob.
/// </summary>
public readonly record struct BlobRef
{
    public const int HashHexLength = 64;

    public string ContentHash { get; }

    [JsonConstructor]
    private BlobRef(string contentHash)
    {
        ArgumentNullException.ThrowIfNull(contentHash);
        if (contentHash.Length != HashHexLength)
            throw new ArgumentException(
                $"Content hash must be {HashHexLength} hex chars, got {contentHash.Length}.",
                nameof(contentHash));
        if (!IsLowercaseHex(contentHash))
            throw new ArgumentException(
                "Content hash must be lowercase hex.", nameof(contentHash));

        ContentHash = contentHash;
    }

    /// <summary>Creates a BlobRef after validating the hash format.</summary>
    public static BlobRef Create(string contentHash) => new(contentHash);

    /// <summary>Two-character shard prefix for filesystem layout.</summary>
    public string Shard => ContentHash[..2];

    public ReadOnlySpan<byte> AsBytes() => Convert.FromHexString(ContentHash);

    public override string ToString() => ContentHash;

    private static bool IsLowercaseHex(string s)
    {
        foreach (var c in s)
            if (!(c is >= '0' and <= '9' or >= 'a' and <= 'f'))
                return false;
        return true;
    }
}
