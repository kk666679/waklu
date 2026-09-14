using System.Security.Cryptography;
using System.Text;

namespace HalalChain.Platform.Api.Modules.Ipfs;

/// <summary>
/// AES-256-GCM encryption for sensitive IPFS payloads. The content
/// is encrypted BEFORE upload, and the CID on-chain is the CID of
/// the ciphertext. The decryption key is held in our secret store
/// and is shared out-of-band with authorised consumers (e.g., a
/// certifier's own vault for cert bodies containing PII).
///
/// On-chain we store the CID of the ciphertext; the plaintext is
/// never published.
/// </summary>
public class StorageEncryption
{
    public const int NonceBytes = 12;
    public const int TagBytes = 16;

    public static (byte[] Ciphertext, byte[] Nonce, byte[] Tag) Encrypt(byte[] plaintext, byte[] key, byte[]? associatedData = null)
    {
        if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes (AES-256).", nameof(key));
        using var aes = new AesGcm(key, TagBytes);
        var nonce = new byte[NonceBytes];
        RandomNumberGenerator.Fill(nonce);
        var ct = new byte[plaintext.Length];
        var tag = new byte[TagBytes];
        aes.Encrypt(nonce, plaintext, ct, tag, associatedData);
        return (ct, nonce, tag);
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] nonce, byte[] tag, byte[]? associatedData = null)
    {
        if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes.", nameof(key));
        using var aes = new AesGcm(key, TagBytes);
        var pt = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, pt, associatedData);
        return pt;
    }

    /// <summary>Encrypt a UTF-8 string and return a single self-contained
    /// payload of (nonce | tag | ciphertext) — the format we write to IPFS
    /// when the consumer doesn't want a multi-file envelope.</summary>
    public static byte[] EncryptToEnvelope(string plaintext, byte[] key, byte[]? associatedData = null)
    {
        var (ct, nonce, tag) = Encrypt(Encoding.UTF8.GetBytes(plaintext), key, associatedData);
        var output = new byte[NonceBytes + TagBytes + ct.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceBytes);
        Buffer.BlockCopy(tag, 0, output, NonceBytes, TagBytes);
        Buffer.BlockCopy(ct, 0, output, NonceBytes + TagBytes, ct.Length);
        return output;
    }

    public static string DecryptFromEnvelope(byte[] envelope, byte[] key, byte[]? associatedData = null)
    {
        if (envelope.Length < NonceBytes + TagBytes) throw new ArgumentException("Envelope too short");
        var nonce = new byte[NonceBytes];
        var tag = new byte[TagBytes];
        var ct = new byte[envelope.Length - NonceBytes - TagBytes];
        Buffer.BlockCopy(envelope, 0, nonce, 0, NonceBytes);
        Buffer.BlockCopy(envelope, NonceBytes, tag, 0, TagBytes);
        Buffer.BlockCopy(envelope, NonceBytes + TagBytes, ct, 0, ct.Length);
        return Encoding.UTF8.GetString(Decrypt(ct, key, nonce, tag, associatedData));
    }

    /// <summary>SHA-256 of (cid || nonce || tag || ciphertext). Recorded on-chain as
    /// <c>metadataHash</c> so tampering with the IPFS gateway response is detectable.</summary>
    public static string ComputeMetadataHash(string cid, byte[] envelope)
    {
        var raw = Encoding.UTF8.GetBytes(cid).Concat(envelope).ToArray();
        return "0x" + Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant();
    }
}
