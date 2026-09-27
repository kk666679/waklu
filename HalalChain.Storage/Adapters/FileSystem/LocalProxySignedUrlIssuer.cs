using System.Security.Cryptography;
using System.Text;
using HalalChain.Application.Storage;
using Microsoft.Extensions.Options;

namespace HalalChain.Storage.Adapters.FileSystem;

/// <summary>
/// Mints HMAC-signed URLs against the API's local blob proxy. Dev and test only —
/// any service holding <see cref="LocalProxyOptions.SigningKey"/> can mint a URL
/// for any blob, so this type must never be deployed. Production uses
/// IAM-scoped presigned URLs issued by the S3 adapter.
/// </summary>
public sealed class LocalProxySignedUrlIssuer : ISignedUrlIssuer
{
    private readonly byte[] _signingKey;
    private readonly string _baseUrl;

    public LocalProxySignedUrlIssuer(IOptions<LocalProxyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.SigningKey))
            throw new InvalidOperationException(
                "LocalProxy:SigningKey must be set. Generate with a CSPRNG.");

        _signingKey = Encoding.UTF8.GetBytes(opts.SigningKey);
        _baseUrl = opts.BaseUrl.TrimEnd('/');
    }

    public Task<Uri> IssueAsync(BlobRef reference, TimeSpan ttl, CancellationToken ct = default)
    {
        var expires = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds();
        var payload = $"{reference.ContentHash}:{expires}";
        var signature = Convert.ToHexString(
            HMACSHA256.HashData(_signingKey, Encoding.UTF8.GetBytes(payload)))
            .ToLowerInvariant();

        var url = $"{_baseUrl}/_blob/{reference.ContentHash}?e={expires}&s={signature}";
        return Task.FromResult(new Uri(url));
    }
}

public sealed class LocalProxyOptions
{
    public const string SectionName = "Blob:LocalProxy";
    public string BaseUrl { get; set; } = "http://localhost:5001";
    public string SigningKey { get; set; } = string.Empty;
}
