namespace HalalChain.Application.Storage;

public interface ISignedUrlIssuer
{
    Task<Uri> IssueAsync(
        BlobRef reference,
        TimeSpan ttl,
        CancellationToken ct = default);
}
