namespace HalalChain.Application.Storage;

/// <summary>
/// A short-lived, signed reference to evidence bytes. The caller follows the URI
/// directly against the blob store — the API is never in the data path.
/// </summary>
public sealed record EvidenceRead(
    Uri SignedUri,
    DateTimeOffset ExpiresAt,
    EvidenceRecord Record);
