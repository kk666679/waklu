using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HalalChain.Platform.Api.Modules.Ipfs;

/// <summary>
/// Local kubo (go-ipfs) daemon adapter. Used in development; the
/// daemon is started by <c>infrastructure/docker-compose.dev.yml</c>.
///
/// Uses the kubo HTTP RPC API (default <c>http://127.0.0.1:5001</c>).
/// In production this adapter is replaced by <see cref="PinataStorageService"/>
/// via the <c>IPFS_PROVIDER</c> config key.
/// </summary>
public sealed class LocalKuboStorageService : IStorageService
{
    private readonly HttpClient _http;
    private readonly string _apiUrl;
    private readonly string _gateway;
    private readonly ContentValidator _validator;
    private readonly ILogger<LocalKuboStorageService> _logger;

    public string ProviderName => "kubo-local";

    public LocalKuboStorageService(HttpClient http, IConfiguration config, ContentValidator validator, ILogger<LocalKuboStorageService> logger)
    {
        _http = http;
        _validator = validator;
        _logger = logger;
        _apiUrl   = (config["IPFS:KuboApiUrl"]  ?? "http://127.0.0.1:5001").TrimEnd('/');
        _gateway  = (config["IPFS:KuboGateway"] ?? "http://127.0.0.1:8080").TrimEnd('/');
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
        }
        using var resp = await _http.PostAsync($"{_apiUrl}/api/v0/add?wrap-with-directory=false&pin=true", form, ct);
        resp.EnsureSuccessStatusCode();
        var respText = await resp.Content.ReadAsStringAsync(ct);
        var respJson = JsonSerializer.Deserialize<KuboAddResponse>(respText);
        if (respJson is null || string.IsNullOrEmpty(respJson.Hash))
            throw new InvalidOperationException("Kubo returned an empty response");
        return new PinResult(respJson.Hash, respJson.Size, ProviderName);
    }

    public async Task<Stream> DownloadAsync(string cid, CancellationToken ct)
    {
        var resp = await _http.GetAsync($"{_gateway}/ipfs/{cid}", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStreamAsync(ct);
    }

    public async Task<bool> IsPinnedAsync(string cid, CancellationToken ct)
    {
        using var resp = await _http.PostAsync($"{_apiUrl}/api/v0/pin/ls?arg={Uri.EscapeDataString(cid)}", new StringContent(""), ct);
        if (!resp.IsSuccessStatusCode) return false;
        var text = await resp.Content.ReadAsStringAsync(ct);
        return !string.IsNullOrWhiteSpace(text) && text.Contains(cid);
    }

    public async Task PinAsync(string cid, CancellationToken ct)
    {
        using var resp = await _http.PostAsync($"{_apiUrl}/api/v0/pin/add?arg={Uri.EscapeDataString(cid)}", new StringContent(""), ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task UnpinAsync(string cid, CancellationToken ct)
    {
        using var resp = await _http.PostAsync($"{_apiUrl}/api/v0/pin/rm?arg={Uri.EscapeDataString(cid)}", new StringContent(""), ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<CidMetadata?> GetMetadataAsync(string cid, CancellationToken ct)
    {
        using var resp = await _http.PostAsync($"{_apiUrl}/api/v0/files/stat?arg={Uri.EscapeDataString(cid)}", new StringContent(""), ct);
        if (!resp.IsSuccessStatusCode) return null;
        try
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            var stat = JsonSerializer.Deserialize<KuboStatResponse>(text);
            return stat is null ? null : new CidMetadata(cid, stat.Size, "unknown", DateTimeOffset.UtcNow);
        }
        catch { return null; }
    }

    public async Task<bool> VerifyAsync(string cid, byte[] expectedBytes, CancellationToken ct)
    {
        await using var stream = await DownloadAsync(cid, ct);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.Length == expectedBytes.Length && ms.ToArray().SequenceEqual(expectedBytes);
    }

    private sealed class KuboAddResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("Hash")] public string? Hash { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("Size")] public long Size { get; set; }
    }
    private sealed class KuboStatResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("Size")] public long Size { get; set; }
    }
}
