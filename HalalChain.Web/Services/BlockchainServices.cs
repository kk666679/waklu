using System.Net.Http.Json;
using System.Text.Json;

namespace HalalChain.Services;

// IPFS Storage Service — thin client over the platform API's real
// IStorageService implementation (Pinata / kubo). It used to fabricate
// random "ipfs://Qm..." CIDs from Guid bytes; that is now removed. Calls
// go through the platform API so the canonical IPFS implementation owns
// the upload.
public interface IPfsService
{
    Task<string> UploadJsonAsync(object data);
    Task<string> UploadImageAsync(byte[] imageBytes, string filename);
}

public class PfsService : IPfsService
{
    private readonly HttpClient _http;
    private readonly ILogger<PfsService> _logger;
    private readonly string _baseAddress;
    private readonly string _apiKey;

    public PfsService(IHttpClientFactory factory, ILogger<PfsService> logger)
    {
        _http = factory.CreateClient("PlatformApiPfs");
        _logger = logger;
        _baseAddress = _http.BaseAddress?.ToString().TrimEnd('/') ?? "";
        _apiKey = Environment.GetEnvironmentVariable("PLATFORM_API__APIKEY") ?? "";
    }

    public async Task<string> UploadJsonAsync(object data)
    {
        return await UploadAsync(JsonSerializer.Serialize(data), "application/json", "metadata.json");
    }

    public async Task<string> UploadImageAsync(byte[] imageBytes, string filename)
    {
        var contentType = filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
            : filename.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp"
            : "image/jpeg";
        var b64 = Convert.ToBase64String(imageBytes);
        return await UploadAsync(b64, contentType, filename);
    }

    private async Task<string> UploadAsync(string payload, string contentType, string filename)
    {
        if (string.IsNullOrEmpty(_baseAddress))
        {
            throw new InvalidOperationException(
                "Platform API base address is not configured. Set PlatformApi:BaseUrl.");
        }
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ipfs/upload")
        {
            Content = JsonContent.Create(new { name = filename, contentType, payload }),
        };
        if (!string.IsNullOrEmpty(_apiKey))
            req.Headers.Add("X-API-Key", _apiKey);

        using var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            _logger.LogError("IPFS upload via platform API failed: {Status} {Body}", resp.StatusCode, body);
            throw new HttpRequestException($"IPFS upload failed: {(int)resp.StatusCode}");
        }
        using var stream = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var cid = doc.RootElement.GetProperty("cid").GetString();
        return $"ipfs://{cid}";
    }
}

// Badge data models
public record BadgeInfo(
    Guid ProductId,
    long TokenId,
    bool IsValid,
    string CertificationBody,
    DateTime IssueDate,
    DateTime ExpiryDate,
    string MetadataUri,
    string EtherscanLink
);

public record BadgeMetadata
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Image { get; init; } = "";
    public List<BadgeAttribute> Attributes { get; init; } = [];
    public List<BadgeEvidence> Evidence { get; init; } = [];
}

public record BadgeAttribute(string TraitType, string Value);
public record BadgeEvidence(string Type, string Hash, string Url);

// Blockchain Badge Service
public interface IBadgeService
{
    Task<string> MintBadgeAsync(Guid productId, string vendorWallet, CancellationToken ct = default);
    Task RevokeBadgeAsync(Guid productId, string reason, CancellationToken ct = default);
    Task<bool> IsBadgeValidAsync(Guid productId, CancellationToken ct = default);
    Task<BadgeInfo?> GetBadgeInfoAsync(Guid productId, CancellationToken ct = default);
}

public class BlockchainBadgeService : IBadgeService
{
    private readonly IPfsService _ipfs;
    private readonly ILogger<BlockchainBadgeService> _logger;
    private readonly Dictionary<Guid, BadgeInfo> _badgeStore = new();
    private long _nextTokenId = 1;

    public BlockchainBadgeService(IPfsService ipfs, ILogger<BlockchainBadgeService> logger)
    {
        _ipfs = ipfs;
        _logger = logger;
    }

    public async Task<string> MintBadgeAsync(Guid productId, string vendorWallet, CancellationToken ct = default)
    {
        var tokenId = _nextTokenId++;
        var metadata = new BadgeMetadata
        {
            Name = $"HalalChain Verification Badge #{tokenId}",
            Description = "This product has been verified halal by the Tawheed AI multi-agent system.",
            Image = "ipfs://QmHalalBadgeImage",
            Attributes =
            [
                new("Product ID", productId.ToString()),
                new("Vendor Wallet", vendorWallet),
                new("Status", "Verified Halal"),
                new("Issue Date", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                new("Expiry Date", DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd")),
                new("Network", "Polygon")
            ],
            Evidence =
            [
                new("Ingredient Analysis", "QmEvidence1", "ipfs://QmEvidence1"),
                new("Certificate Validation", "QmEvidence2", "ipfs://QmEvidence2"),
                new("Supply Chain Audit", "QmEvidence3", "ipfs://QmEvidence3")
            ]
        };

        string metadataUri;
        try
        {
            metadataUri = await _ipfs.UploadJsonAsync(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload badge metadata via platform API; aborting mint");
            throw;
        }

        var badge = new BadgeInfo(
            productId, tokenId, true,
            "Tawheed AI System",
            DateTime.UtcNow, DateTime.UtcNow.AddYears(1),
            metadataUri,
            $"https://polygonscan.com/token/0x0000000000000000000000000000000000000000?a={tokenId}"
        );

        _badgeStore[productId] = badge;
        _logger.LogInformation("Badge minted: ProductId={ProductId}, TokenId={TokenId}", productId, tokenId);

        return tokenId.ToString();
    }

    public Task RevokeBadgeAsync(Guid productId, string reason, CancellationToken ct = default)
    {
        if (_badgeStore.TryGetValue(productId, out var badge))
        {
            _badgeStore[productId] = badge with { IsValid = false };
            _logger.LogInformation("Badge revoked: ProductId={ProductId}, Reason={Reason}", productId, reason);
        }
        return Task.CompletedTask;
    }

    public Task<bool> IsBadgeValidAsync(Guid productId, CancellationToken ct = default)
    {
        if (_badgeStore.TryGetValue(productId, out var badge))
            return Task.FromResult(badge.IsValid && badge.ExpiryDate > DateTime.UtcNow);
        return Task.FromResult(false);
    }

    public Task<BadgeInfo?> GetBadgeInfoAsync(Guid productId, CancellationToken ct = default)
    {
        _badgeStore.TryGetValue(productId, out var badge);
        return Task.FromResult(badge);
    }
}
