namespace HalalChain.Application.Storage;

public sealed record EvidenceRecord(
    EvidenceId Id,
    BlobRef Blob,
    EvidenceDescriptor Descriptor,
    DateTimeOffset IngestedAt);
