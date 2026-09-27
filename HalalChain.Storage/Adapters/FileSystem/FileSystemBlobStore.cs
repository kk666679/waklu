using System.Security.Cryptography;
using HalalChain.Application.Storage;
using HalalChain.Storage.Integrity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HalalChain.Storage.Adapters.FileSystem;

public sealed class FileSystemBlobStore : IBlobStore
{
    private const int BufferSize = 81_920;

    private readonly string _root;
    private readonly bool _verifyOnRead;
    private readonly ILogger<FileSystemBlobStore> _log;

    public FileSystemBlobStore(
        IOptions<FileSystemOptions> options,
        ILogger<FileSystemBlobStore> log)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(log);

        _root = Path.GetFullPath(options.Value.Root);
        _verifyOnRead = options.Value.VerifyOnRead;
        _log = log;

        Directory.CreateDirectory(_root);
    }

    public async Task<BlobRef> PutAsync(
        Stream content,
        BlobMetadata meta,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(meta);

        // Spool to a temp file while hashing in a single pass. The content
        // hash is the storage address, so it cannot be known until the last
        // byte is read — and the bytes still have to land on disk. Spooling
        // keeps this correct for non-seekable streams and keeps peak memory
        // flat regardless of evidence size.
        var spoolRoot = Path.Combine(_root, ".spool");
        Directory.CreateDirectory(spoolRoot);
        var tmp = Path.Combine(spoolRoot, $"{Guid.NewGuid():N}.tmp");

        BlobRef reference;
        try
        {
            reference = await SpoolAndHashAsync(content, tmp, ct).ConfigureAwait(false);
        }
        catch
        {
            TryDelete(tmp);
            throw;
        }

        var path = PathFor(reference);
        if (File.Exists(path))
        {
            _log.LogDebug("Blob already present at {Path}", path);
            TryDelete(tmp);
            return reference;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        try
        {
            File.Move(tmp, path, overwrite: false);
        }
        catch (IOException) when (File.Exists(path))
        {
            // Lost the race against a concurrent writer. The winner's file
            // stands; content addressing means it is byte-identical anyway.
            TryDelete(tmp);
        }

        return reference;
    }

    public async Task<Stream?> OpenReadAsync(
        BlobRef reference,
        CancellationToken ct = default)
    {
        var path = PathFor(reference);
        if (!File.Exists(path)) return null;

        if (_verifyOnRead)
        {
            await using var verify = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: BufferSize, useAsync: true);
            var actual = await SHA256HexAsync(verify, ct).ConfigureAwait(false);
            if (!string.Equals(actual, reference.ContentHash, StringComparison.Ordinal))
            {
                _log.LogError(
                    "Integrity failure on {Path}: expected {Expected}, got {Actual}",
                    path, reference.ContentHash, actual);
                throw new HashMismatchException(reference, actual);
            }
        }

        return new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: BufferSize, useAsync: true);
    }

    public Task<bool> ExistsAsync(BlobRef reference, CancellationToken ct = default)
        => Task.FromResult(File.Exists(PathFor(reference)));

    private static async Task<BlobRef> SpoolAndHashAsync(
        Stream content, string tmp, CancellationToken ct)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[BufferSize];

        await using (var fs = new FileStream(
            tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: BufferSize, useAsync: true))
        {
            int read;
            while ((read = await content.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                hasher.AppendData(buffer, 0, read);
                await fs.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            }
            await fs.FlushAsync(ct).ConfigureAwait(false);
        }

        return BlobRef.Create(Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant());
    }

    private string PathFor(BlobRef reference)
        => Path.Combine(_root, reference.Shard, reference.ContentHash);

    private static async Task<string> SHA256HexAsync(Stream content, CancellationToken ct)
    {
        var hash = await SHA256.HashDataAsync(content, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            // A leaked spool file is harmless; it is excluded from ExistsAsync
            // and cleaned by the next sweep. Never mask the original failure.
        }
    }
}
