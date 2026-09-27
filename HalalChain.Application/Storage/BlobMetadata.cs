namespace HalalChain.Application.Storage;

public sealed record BlobMetadata(
    string ContentType,
    long SizeBytes,
    string? OriginalFilename,
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<string, string> Tags)
{
    public static BlobMetadata Create(
        string contentType,
        long sizeBytes,
        string? originalFilename,
        DateTimeOffset createdAt,
        IReadOnlyDictionary<string, string>? tags = null)
        => new(contentType, sizeBytes, originalFilename, createdAt,
               tags ?? new Dictionary<string, string>());
}
