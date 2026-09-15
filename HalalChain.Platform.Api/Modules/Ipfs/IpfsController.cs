using HalalChain.Platform.Contracts.Api.Errors;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Contracts.Storage.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalChain.Platform.Api.Modules.Ipfs;

/// <summary>
/// HTTP façade over the provider-agnostic <see cref="IStorageService"/>.
/// Uploads go through the registered storage backend (Pinata in prod,
/// local kubo in dev, local fallback otherwise). Downloads stream the
/// raw content by CID so the Web front-end's <c>PfsService</c> and the
/// blockchain badge flow can resolve content without a separate gateway.
/// </summary>
[ApiController]
[Route("api/v1/ipfs")]
[Authorize(Roles = $"{AuthConstants.RoleVendor},{AuthConstants.RoleAdmin}")]
[ApiVersion("1.0")]
public sealed class IpfsController(IStorageService storage, ILogger<IpfsController> logger) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<ActionResult<IpfsUploadResponse>> Upload(
        [FromBody] IpfsUploadRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ErrorResponse("INVALID_NAME", "Name is required."));

        if (string.IsNullOrWhiteSpace(request.ContentType))
            return BadRequest(new ErrorResponse("INVALID_CONTENT_TYPE", "Content type is required."));

        if (request.Payload is null)
            return BadRequest(new ErrorResponse("INVALID_PAYLOAD", "Payload is required."));

        Stream content;
        if (IsLikelyBase64(request.Payload))
        {
            try
            {
                var bytes = Convert.FromBase64String(request.Payload);
                content = new MemoryStream(bytes);
            }
            catch (FormatException)
            {
                return BadRequest(new ErrorResponse("INVALID_PAYLOAD", "Payload is not valid base64."));
            }
        }
        else
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(request.Payload);
            content = new MemoryStream(bytes);
        }

        await using (content)
        {
            var uploadReq = new UploadRequest(request.Name, content, request.ContentType);

            try
            {
                var result = await storage.UploadAsync(uploadReq, ct);
                return Ok(new IpfsUploadResponse(result.Cid, result.SizeBytes, result.Provider));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "IPFS upload failed for {Name}", request.Name);
                return StatusCode(500,
                    new ErrorResponse("IPFS_UPLOAD_FAILED", "Upload failed. See server logs."));
            }
        }
    }

    [HttpGet("{cid}")]
    [AllowAnonymous]
    public async Task<ActionResult> Download(string cid, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cid))
            return BadRequest(new ErrorResponse("INVALID_CID", "CID is required."));

        Stream? stream;
        try
        {
            stream = await storage.DownloadAsync(cid, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "IPFS download failed for CID {Cid}", cid);
            return StatusCode(500, new ErrorResponse("IPFS_DOWNLOAD_FAILED", "Download failed."));
        }

        if (stream is null)
            return NotFound(new ErrorResponse("CID_NOT_FOUND", $"CID '{cid}' not found."));

        var metadata = await storage.GetMetadataAsync(cid, ct);
        var contentType = metadata?.ContentType ?? "application/octet-stream";

        return new FileStreamResult(stream, contentType)
        {
            FileDownloadName = cid
        };
    }

    [HttpGet("{cid}/metadata")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetMetadata(string cid, CancellationToken ct)
    {
        var metadata = await storage.GetMetadataAsync(cid, ct);
        if (metadata is null)
            return NotFound(new ErrorResponse("CID_NOT_FOUND", $"CID '{cid}' not found."));

        var isPinned = await storage.IsPinnedAsync(cid, ct);
        return Ok(new
        {
            metadata.Cid,
            metadata.SizeBytes,
            ContentType = metadata.ContentType,
            metadata.PinnedAt,
            IsPinned = isPinned,
            Provider = storage.ProviderName
        });
    }

    private static bool IsLikelyBase64(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length % 4 != 0)
            return false;

        if (input.Contains('\n') || input.Contains('\r') || input.Contains(' '))
            return false;

        return System.Text.RegularExpressions.Regex.IsMatch(input, @"^[A-Za-z0-9+/]*={0,2}$");
    }
}
