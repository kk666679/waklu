using HalalChain.Application.Storage;
using HalalChain.Storage.Adapters.FileSystem;
using HalalChain.Storage.Integrity;
using HalalChain.Storage.Tests.Contract;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HalalChain.Storage.Tests;

public sealed class FileSystemBlobStoreTests : BlobStoreContractTests, IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), $"halalchain-blob-{Guid.NewGuid():N}");

    protected override IBlobStore CreateStore()
        => new FileSystemBlobStore(
            Options.Create(new FileSystemOptions { Root = _root, VerifyOnRead = false }),
            NullLogger<FileSystemBlobStore>.Instance);

    [Fact]
    public async Task Put_writes_under_shard_directory()
    {
        var store = CreateStore();
        var reference = await store.PutAsync(
            new MemoryStream("x"u8.ToArray()),
            BlobMetadata.Create("text/plain", 1, "x.txt", DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        var expected = Path.Combine(_root, reference.Shard, reference.ContentHash);
        Assert.True(File.Exists(expected));
    }

    [Fact]
    public async Task VerifyOnRead_rejects_tampered_blob()
    {
        var store = new FileSystemBlobStore(
            Options.Create(new FileSystemOptions { Root = _root, VerifyOnRead = true }),
            NullLogger<FileSystemBlobStore>.Instance);

        var reference = await store.PutAsync(
            new MemoryStream("original"u8.ToArray()),
            BlobMetadata.Create("text/plain", 8, "o.txt", DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        // Tamper on disk
        var path = Path.Combine(_root, reference.Shard, reference.ContentHash);
        await File.WriteAllTextAsync(path, "tampered!", TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<HashMismatchException>(
            async () => await store.OpenReadAsync(reference, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task VerifyOnRead_disabled_by_default_serves_tampered_bytes()
    {
        // Documents the cost trade-off: without verification the adapter is a
        // dumb byte server. Production sets VerifyOnRead where integrity matters.
        var store = new FileSystemBlobStore(
            Options.Create(new FileSystemOptions { Root = _root, VerifyOnRead = false }),
            NullLogger<FileSystemBlobStore>.Instance);

        var reference = await store.PutAsync(
            new MemoryStream("original"u8.ToArray()),
            BlobMetadata.Create("text/plain", 8, "o.txt", DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        var path = Path.Combine(_root, reference.Shard, reference.ContentHash);
        await File.WriteAllTextAsync(path, "tampered!", TestContext.Current.CancellationToken);

        await using var read = await store.OpenReadAsync(reference, TestContext.Current.CancellationToken);
        Assert.NotNull(read);
        using var reader = new StreamReader(read!);
        Assert.Equal("tampered!", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Put_leaves_no_spool_files_behind()
    {
        var store = CreateStore();
        await store.PutAsync(new MemoryStream("spooled"u8.ToArray()),
            BlobMetadata.Create("text/plain", 7, "s.txt", DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        var spool = Path.Combine(_root, ".spool");
        if (Directory.Exists(spool))
            Assert.Empty(Directory.GetFiles(spool));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
