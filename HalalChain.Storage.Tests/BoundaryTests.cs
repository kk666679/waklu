using HalalChain.Application.Storage;
using HalalChain.Storage.Adapters.FileSystem;
using HalalChain.Storage.Evidence;
using Microsoft.Extensions.Options;
using Xunit;

namespace HalalChain.Storage.Tests;

public sealed class SignedUrlIssuerTests
{
    private static LocalProxySignedUrlIssuer Create(string key = "unit-test-key")
        => new(Options.Create(new LocalProxyOptions
        {
            BaseUrl = "http://localhost:5001/",
            SigningKey = key
        }));

    [Fact]
    public async Task Issues_a_url_under_the_blob_proxy_route()
    {
        var issuer = Create();
        var reference = BlobRef.Create(new string('a', 64));

        var uri = await issuer.IssueAsync(reference, TimeSpan.FromMinutes(5));

        Assert.Equal("/_blob/" + reference.ContentHash, uri.AbsolutePath);
        Assert.Contains("e=", uri.Query, StringComparison.Ordinal);
        Assert.Contains("s=", uri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Same_reference_and_ttl_produce_the_same_signature()
    {
        var issuer = Create();
        var reference = BlobRef.Create(new string('b', 64));
        var ttl = TimeSpan.FromMinutes(5);

        var first = await issuer.IssueAsync(reference, ttl);
        var second = await issuer.IssueAsync(reference, ttl);

        Assert.Equal(first.Query, second.Query);
    }

    [Fact]
    public async Task Different_keys_produce_different_signatures()
    {
        var reference = BlobRef.Create(new string('c', 64));
        var ttl = TimeSpan.FromMinutes(5);

        var a = await Create("key-one").IssueAsync(reference, ttl);
        var b = await Create("key-two").IssueAsync(reference, ttl);

        Assert.NotEqual(a.Query, b.Query);
    }

    [Fact]
    public void Blank_signing_key_fails_closed()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new LocalProxySignedUrlIssuer(
                Options.Create(new LocalProxyOptions { SigningKey = "  " })));

        Assert.Contains("SigningKey", ex.Message, StringComparison.Ordinal);
    }
}

public sealed class NdjsonAccessLoggerTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), $"halalchain-log-{Guid.NewGuid():N}");

    private string LogPath => System.IO.Path.Combine(_root, "nested", "access-log.ndjson");

    private NdjsonAccessLogger Create()
        => new(Options.Create(new NdjsonAccessLogOptions { Path = LogPath }));

    [Fact]
    public async Task Appends_one_line_per_record()
    {
        var logger = Create();

        await logger.RecordAsync(new AccessRecord(
            AccessKind.Ingest, EvidenceId.New(), BlobRef.Create(new string('d', 64)),
            "actor-1", DateTimeOffset.UtcNow, "Certificate"));
        await logger.RecordAsync(new AccessRecord(
            AccessKind.Read, EvidenceId.New(), BlobRef.Create(new string('e', 64)),
            "actor-2", DateTimeOffset.UtcNow, null));

        var lines = await File.ReadAllLinesAsync(LogPath);
        Assert.Equal(2, lines.Length);
    }

    [Fact]
    public async Task Concurrent_writers_do_not_interleave_lines()
    {
        var logger = Create();

        await Task.WhenAll(Enumerable.Range(0, 50).Select(i => logger.RecordAsync(
            new AccessRecord(
                AccessKind.Read, EvidenceId.New(), BlobRef.Create(new string('f', 64)),
                $"actor-{i}", DateTimeOffset.UtcNow, null))));

        var lines = await File.ReadAllLinesAsync(LogPath);
        Assert.Equal(50, lines.Length);
        Assert.All(lines, l => Assert.StartsWith("{", l));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}

public sealed class StorageBoundaryTests
{
    [Fact]
    public void Storage_assembly_declares_no_merkle_tree_type()
    {
        // Merge decision M1. Keccak256 for on-chain Merkle internal nodes lives
        // in the blockchain module. A SHA-256 Merkle tree here is the exact bug
        // that made every inclusion proof fail on first integration.
        var offenders = typeof(Storage.DependencyInjection.StorageServiceCollectionExtensions)
            .Assembly
            .GetTypes()
            .Where(t => t.Name.Contains("Merkle", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.FullName!)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Storage_assembly_does_not_reference_Nethereum()
    {
        var referenced = typeof(Storage.DependencyInjection.StorageServiceCollectionExtensions)
            .Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        Assert.DoesNotContain(
            referenced,
            n => n.Contains("Nethereum", StringComparison.OrdinalIgnoreCase));
    }
}
