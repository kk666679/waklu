using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HalalChain.Platform.Api.Modules.Ipfs;

/// <summary>
/// Pinata-backed <see cref="IStorageService"/>. Pinata is our default
/// production provider: it offers IPFS pinning, a dedicated gateway
/// (with private signed URLs), and good SLAs.
///
/// Auth: a JWT issued from the Pinata dashboard. Configured via
/// <c>IPFS:PinataJwt</c>. The JWT MUST be loaded from a secret
/// manager in production; this class never logs it.
///
/// Wire format: Pinata's "pinFileToIPFS" endpoint expects
/// <c>multipart/form-data</c> with the file under "file" and an
/// optional "pinataMetadata" field.
/// </summary>
public sealed class PinataStorageService : IStorageService
{
    private readonly HttpClient _http;
    private readonly string _jwt;
    private readonly string _gateway;
    private readonly ContentValidator _validator;
    private readonly ILogger<PinataStorageService> _logger;

    public string ProviderName => "pinata";

    public PinataStorageService(HttpClient http, IConfiguration config, ContentValidator validator, ILogger<PinataStorageService> logger)
    {
        _http = http;
        _validator = validator;
        _logger = logger;
        _jwt = config["IPFS:PinataJwt"]
            ?? throw new InvalidOperationException("IPFS:PinataJwt is not configured.");
        _gateway = (config["IPFS:PublicGateway"] ?? "https://gateway.pinata.cloud").TrimEnd('/');
    }

    public async Task<PinResult> UploadAsync(UploadRequest req, CancellationToken ct)
    {
        var v = await _validator.ValidateAsync(req, ct);
        if (!v.IsValid) throw new InvalidOperationException($"Upload rejected: {v.Error}");

        using var form = new MultipartFormDataContent("----" + Guid.NewGuid().ToString("N"));
        await using (var stream = req.Content)
        {
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(req.ContentType);
            form.Add(fileContent, "file", req.Name ?? "file");

            if (req.Metadata is { Count: > 0 })
            {
                var meta = new
                {
                    name = req.Name,
                    keyvalues = req.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value)
                };
                var metaJson = JsonSerializer.Serialize(meta);
                form.Add(new StringContent(metaJson, Encoding.UTF8, "application/json"), "pinataMetadata");
            }
        }

        using var msg = new HttpRequestMessage(HttpMethod.Post, "https://api.pinata.cloud/pinning/pinFileToIPFS")
        {
            Content = form
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);

        using var resp = await _http.SendAsync(msg, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Pinata upload failed: {Status} {Body}", resp.StatusCode, body);
            throw new InvalidOperationException($"Pinata upload failed: {(int)resp.StatusCode}");
        }
        var result = await resp.Content.ReadFromJsonAsync<PinataPinResponse>(cancellationToken: ct);
        if (result is null || string.IsNullOrEmpty(result.IpfsHash))
            throw new InvalidOperationException("Pinata returned an empty response");
        return new PinResult(result.IpfsHash, result.PinSize ?? 0, ProviderName);
    }

    public async Task<Stream> DownloadAsync(string cid, CancellationToken ct)
    {
        var resp = await _http.GetAsync($"{_gateway}/ipfs/{cid}", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStreamAsync(ct);
    }

    public async Task<bool> IsPinnedAsync(string cid, CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.pinata.cloud/pinning/pinners?hashContains={cid}&status=pinned");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);
        using var resp = await _http.SendAsync(msg, ct);
        if (!resp.IsSuccessStatusCode) return false;
        var body = await resp.Content.ReadFromJsonAsync<PinataListResponse>(cancellationToken: ct);
        return body?.Rows?.Any(r => r.IpfsPin?.Cid == cid) ?? false;
    }

    public async Task PinAsync(string cid, CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, "https://api.pinata.cloud/pinning/pinByHash");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);
        msg.Content = new StringContent(JsonSerializer.Serialize(new { hashToPin = cid }), Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(msg, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task UnpinAsync(string cid, CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Delete, $"https://api.pinata.cloud/pinning/unpin/{cid}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);
        using var resp = await _http.SendAsync(msg, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<CidMetadata?> GetMetadataAsync(string cid, CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.pinata.cloud/pinning/pinners?hashContains={cid}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);
        using var resp = await _http.SendAsync(msg, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var body = await resp.Content.ReadFromJsonAsync<PinataListResponse>(cancellationToken: ct);
        var pin = body?.Rows?.FirstOrDefault(r => r.IpfsPin?.Cid == cid)?.IpfsPin;
        return pin is null ? null : new CidMetadata(cid, pin.Size ?? 0, "unknown", pin.PinnedAt ?? DateTimeOffset.UtcNow);
    }

    public async Task<bool> VerifyAsync(string cid, byte[] expectedBytes, CancellationToken ct)
    {
        // Re-download and compare byte-for-byte. Pinata is the trusted gateway.
        await using var stream = await DownloadAsync(cid, ct);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.Length == expectedBytes.Length && ms.ToArray().SequenceEqual(expectedBytes);
    }

    private sealed class PinataPinResponse { [System.Text.Json.Serialization.JsonPropertyName("IpfsHash")] public string? IpfsHash { get; set; } [System.Text.Json.Serialization.JsonPropertyName("PinSize")] public long? PinSize { get; set; } }
    private sealed class PinataListResponse { [System.Text.Json.Serialization.JsonPropertyName("rows")] public List<PinataPinRow>? Rows { get; set; } }
    private sealed class PinataPinRow { [System.Text.Json.Serialization.JsonPropertyName("ipfs_pin")] public PinataPinEntry? IpfsPin { get; set; } }
    private sealed class PinataPinEntry { [System.Text.Json.Serialization.JsonPropertyName("cid")] public string? Cid { get; set; } [System.Text.Json.Serialization.JsonPropertyName("size")] public long? Size { get; set; } [System.Text.Json.Serialization.JsonPropertyName("pinned_at")] public DateTimeOffset? PinnedAt { get; set; } }
}
