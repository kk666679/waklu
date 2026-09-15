namespace HalalChain.Platform.Contracts.Storage.Dto;

public sealed record IpfsUploadRequest(
    string Name,
    string ContentType,
    string Payload);

public sealed record IpfsUploadResponse(
    string Cid,
    long SizeBytes,
    string Provider);
