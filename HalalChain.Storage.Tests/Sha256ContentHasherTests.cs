using HalalChain.Storage.Integrity;
using Xunit;

namespace HalalChain.Storage.Tests;

public sealed class Sha256ContentHasherTests
{
    private readonly Sha256ContentHasher _hasher = new();

    [Fact]
    public async Task Known_input_produces_known_hash()
    {
        // SHA-256("hello") = 2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824
        var reference = await _hasher.ComputeAsync(new MemoryStream("hello"u8.ToArray()), TestContext.Current.CancellationToken);
        Assert.Equal(
            "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824",
            reference.ContentHash);
    }

    [Fact]
    public async Task Hash_is_lowercase_hex_64_chars()
    {
        var reference = await _hasher.ComputeAsync(new MemoryStream([1, 2, 3]), TestContext.Current.CancellationToken);
        Assert.Equal(64, reference.ContentHash.Length);
        Assert.Matches("^[0-9a-f]{64}$", reference.ContentHash);
    }

    [Fact]
    public async Task Different_streams_produce_different_hashes()
    {
        var a = await _hasher.ComputeAsync(new MemoryStream("a"u8.ToArray()), TestContext.Current.CancellationToken);
        var b = await _hasher.ComputeAsync(new MemoryStream("b"u8.ToArray()), TestContext.Current.CancellationToken);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task Empty_stream_hashes_to_the_empty_digest()
    {
        var reference = await _hasher.ComputeAsync(new MemoryStream(), TestContext.Current.CancellationToken);
        Assert.Equal(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            reference.ContentHash);
    }

    [Fact]
    public async Task Hash_is_stable_across_calls_for_the_same_bytes()
    {
        var payload = "stability"u8.ToArray();
        var a = await _hasher.ComputeAsync(new MemoryStream(payload), TestContext.Current.CancellationToken);
        var b = await _hasher.ComputeAsync(new MemoryStream(payload), TestContext.Current.CancellationToken);
        Assert.Equal(a, b);
    }
}
