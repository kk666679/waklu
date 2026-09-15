using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace HalalChain.Platform.Api.Modules.Ipfs;

/// <summary>
/// Fallback <see cref="IStorageService"/> that stores content on the local
/// file system (or in-memory in test mode). Used when no cloud IPFS provider
/// (Pinata / kubo) is configured, so the upload/download API surface is
/// always available in development and CI.
///
/// CIDs are SHA-256 multihash-style hex strings derived from the content.
/// Not content-addressed in the true IPFS sense (no merkle/peer resolution),
/// but deterministic and suitable for local workflows.
/// </summary>
public sealed class LocalFileStorageService : IStorageService
{
    private readonly string? _rootPath;
    private readonly ConcurrentDictionary<string, byte[]> _memory = new();
    private readonly ILogger<LocalFileStorageService> _logger;

    public string ProviderName => "local-fallback";

    public LocalFileStorageService(IConfiguration config, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _rootPath = config["IPFS:LocalPath"];
        if (!string.IsNullOrWhiteSpace(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
            _logger.LogInformation("Local fallback IPFS storage rooted at {Path}", _rootPath);
        }
        else
        {
            _logger.LogInformation("Local fallback IPFS storage running in-memory (no local path configured).");
        }
    }

    private static string ComputeCid(byte[] bytes) =>
        "sha256-" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private async Task StoreAsync(string cid, byte[] bytes)
    {
        if (_rootPath is not null)
        {
            var path = Path.Combine(_rootPath, cid);
            await File.WriteAllBytesAsync(path, bytes);
        }
        else
        {
            _memory[cid] = bytes;
        }
    }

    private async Task<byte[]?> RetrieveAsync(string cid)
    {
        if (_rootPath is not null)
        {
            var path = Path.Combine(_rootPath, cid);
            return File.Exists(path) ? await File.ReadAllBytesAsync(path) : null;
        }
        return _memory.TryGetValue(cid, out var bytes) ? bytes : null;
    }

    public async Task<PinResult> UploadAsync(UploadRequest req, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        await req.Content.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        var cid = ComputeCid(bytes);
        await StoreAsync(cid, bytes);
        return new PinResult(cid, bytes.Length, ProviderName);
    }

    public async Task<Stream> DownloadAsync(string cid, CancellationToken ct)
    {
        var bytes = await RetrieveAsync(cid);
        if (bytes is null)
            throw new FileNotFoundException($"CID '{cid}' not found in local storage.");
        return new MemoryStream(bytes);
    }

    public async Task<bool> IsPinnedAsync(string cid, CancellationToken ct) =>
        _rootPath is not null
            ? File.Exists(Path.Combine(_rootPath, cid))
            : _memory.ContainsKey(cid);

    public Task PinAsync(string cid, CancellationToken ct) => Task.CompletedTask;

    public async Task UnpinAsync(string cid, CancellationToken ct)
    {
        if (_rootPath is not null)
        {
            var path = Path.Combine(_rootPath, cid);
            if (File.Exists(path)) File.Delete(path);
        }
        else
        {
            _memory.TryRemove(cid, out _);
        }
    }

    public async Task<CidMetadata?> GetMetadataAsync(string cid, CancellationToken ct)
    {
        var bytes = await RetrieveAsync(cid);
        if (bytes is null) return null;
        return new CidMetadata(cid, bytes.Length, "application/octet-stream", DateTimeOffset.UtcNow);
    }

    public async Task<bool> VerifyAsync(string cid, byte[] expectedBytes, CancellationToken ct)
    {
        var bytes = await RetrieveAsync(cid);
        return bytes is not null && bytes.Length == expectedBytes.Length && bytes.SequenceEqual(expectedBytes);
    }
}
