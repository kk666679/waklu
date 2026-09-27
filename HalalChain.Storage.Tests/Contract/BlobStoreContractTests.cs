using HalalChain.Application.Storage;
using Xunit;

namespace HalalChain.Storage.Tests.Contract;

/// <summary>
/// Every IBlobStore adapter must pass this suite. This is what keeps
/// FileSystem, S3, Azure, and IPFS behaviourally interchangeable.
/// </summary>
public abstract class BlobStoreContractTests : IAsyncLifetime
{
    protected abstract IBlobStore CreateStore();
    protected IBlobStore Store { get; private set; } = null!;

    public ValueTask InitializeAsync()
    {
        Store = CreateStore();
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static BlobMetadata Meta(string contentType = "application/pdf",
        string? filename = "cert.pdf")
        => BlobMetadata.Create(contentType, 0, filename, DateTimeOffset.UtcNow);

    private static MemoryStream Bytes(string s)
        => new(System.Text.Encoding.UTF8.GetBytes(s));

    [Fact]
    public async Task Put_then_OpenRead_roundtrips_content()
    {
        var payload = "halal-certificate-payload";
        var reference = await Store.PutAsync(Bytes(payload), Meta());

        await using var read = await Store.OpenReadAsync(reference);
        Assert.NotNull(read);
        using var reader = new StreamReader(read!);
        Assert.Equal(payload, await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Put_is_content_addressed_identical_content_same_ref()
    {
        var payload = "identical";
        var r1 = await Store.PutAsync(Bytes(payload), Meta());
        var r2 = await Store.PutAsync(Bytes(payload), Meta());
        Assert.Equal(r1, r2);
    }

    [Fact]
    public async Task Put_is_idempotent_when_blob_already_exists()
    {
        var payload = "dedupe-me";
        var r1 = await Store.PutAsync(Bytes(payload), Meta());
        var r2 = await Store.PutAsync(Bytes(payload), Meta());
        Assert.Equal(r1.ContentHash, r2.ContentHash);

        await using var read = await Store.OpenReadAsync(r2);
        Assert.NotNull(read);
        using var reader = new StreamReader(read!);
        Assert.Equal(payload, await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task OpenRead_returns_null_for_unknown_reference()
    {
        var missing = BlobRef.Create(new string('a', 64));
        var result = await Store.OpenReadAsync(missing);
        Assert.Null(result);
    }

    [Fact]
    public async Task Exists_is_false_for_unknown_reference()
    {
        var missing = BlobRef.Create(new string('b', 64));
        Assert.False(await Store.ExistsAsync(missing));
    }

    [Fact]
    public async Task Exists_is_true_after_put()
    {
        var inserted = await Store.PutAsync(Bytes("new"), Meta());
        Assert.True(await Store.ExistsAsync(inserted));
    }

    [Fact]
    public async Task Different_content_produces_different_refs()
    {
        var r1 = await Store.PutAsync(Bytes("alpha"), Meta());
        var r2 = await Store.PutAsync(Bytes("beta"), Meta());
        Assert.NotEqual(r1, r2);
    }

    [Fact]
    public async Task Put_accepts_non_seekable_streams()
    {
        // The adapter cannot rewind the caller's stream: it spools to disk
        // while hashing. A rewind-based implementation stores zero bytes here.
        var payload = "non-seekable-payload"u8.ToArray();
        using var source = new NonSeekableStream(payload);
        var reference = await Store.PutAsync(source, Meta());

        await using var read = await Store.OpenReadAsync(reference);
        Assert.NotNull(read);
        using var buffer = new MemoryStream();
        await read!.CopyToAsync(buffer);
        Assert.Equal(payload, buffer.ToArray());
    }

    /// <summary>
    /// Structural, not a runtime test: IBlobStore must not declare a Delete
    /// member. This test exists so a future contributor sees the rule.
    /// </summary>
    [Fact]
    public void IBlobStore_declares_no_delete_member()
    {
        var members = typeof(IBlobStore).GetMembers()
            .Select(m => m.Name)
            .Where(n => n.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Remove", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(members);
    }

    private sealed class NonSeekableStream(byte[] payload) : Stream
    {
        private readonly MemoryStream _inner = new(payload);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
