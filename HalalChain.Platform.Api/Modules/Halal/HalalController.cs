using HalalChain.Domain.Halal;
using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Api.Modules.Events;
using HalalChain.Platform.Contracts.Api.Errors;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Contracts.Halal.Dto;
using HalalChain.Platform.Contracts.Halal.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Halal;

[ApiController]
[Route("api/v1/halal")]
[Authorize]
[ApiVersion("1.0")]
public sealed class HalalController(
    HalalChainDbContext db,
    IEventBus eventBus,
    ITawheedClient tawheed,
    ICurrentUser user) : ControllerBase
{
    [HttpPost("products/{productId:guid}/certificates")]
    [Authorize(Roles = $"{AuthConstants.RoleVendor},{AuthConstants.RoleAdmin}")]
    public async Task<ActionResult<HalalCertificateDto>> SubmitCertificate(
        Guid productId, [FromBody] SubmitCertificateRequest request, CancellationToken ct)
    {
        if (request.ExpiryDate <= DateTimeOffset.UtcNow)
            return BadRequest(new ErrorResponse("CERTIFICATE_EXPIRED", "Certificate is already expired."));

        var product = await db.Products.FindAsync([productId], ct);
        if (product is null) return NotFound(new ErrorResponse("PRODUCT_NOT_FOUND", $"Product {productId} not found."));

        // The caller's Guid identity from the JWT sub claim.
        var subjectId = user.RequireUserId();
        if (product.VendorId != subjectId && !User.IsInRole(AuthConstants.RoleAdmin))
            return Forbid();

        var cert = new Certificate
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CertificateNumber = request.CertificateNumber,
            CertificationBody = request.CertificationBody,
            Jurisdiction = request.Jurisdiction,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            Scope = request.Scope,
        };
        db.Certificates.Add(cert);
        await eventBus.PublishAsync(new CertificateSubmittedEvent(productId, cert.Id, cert.CertificateNumber), ct);
        await db.SaveChangesAsync(ct);

        return Ok(new HalalCertificateDto(cert.Id, cert.ProductId, cert.CertificateNumber,
            cert.CertificationBody, cert.Jurisdiction, (Contracts.Halal.Dto.CertificateStatus)cert.Status,
            cert.IssueDate, cert.ExpiryDate, cert.Scope, cert.CreatedAt));
    }

    [HttpPost("products/{productId:guid}/verify")]
    [Authorize(Roles = AuthConstants.RoleVerificationOfficer)]
    public async Task<ActionResult<VerificationDto>> TriggerVerification(
        Guid productId, [FromBody] TriggerVerificationRequest request, CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Vendor)
            .Include(p => p.Certificates)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product is null) return NotFound(new ErrorResponse("PRODUCT_NOT_FOUND", $"Product {productId} not found."));

        // Create initial verification record
        var verification = new HalalVerification
        {
            ProductId = productId,
            PolicyVersion = request.PolicyVersion,
            Jurisdiction = request.Jurisdiction,
        };
        db.HalalVerifications.Add(verification);

        // Publish verification requested event
        await eventBus.PublishAsync(new HalalVerificationRequestedEvent(productId, verification.Id), ct);
        await db.SaveChangesAsync(ct); // Commit verification + outbox

        // Call Tawheed multi-agent verification system
        var latestCert = product.Certificates
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        var tawheedRequest = new TawheedVerificationRequest
        {
            ProductId = productId,
            ProductTitle = product.Title,
            Description = product.Description,
            Ingredients = [], // Could be parsed from product description
            CertificateNumber = latestCert?.CertificateNumber ?? "",
            CertificationBody = latestCert?.CertificationBody ?? "",
            Jurisdiction = request.Jurisdiction,
            PolicyVersion = request.PolicyVersion,
        };

        var tawheedResult = await tawheed.RequestVerificationAsync(tawheedRequest, ct);

        if (tawheedResult.Success)
        {
            // Map Tawheed result to verification entity
            verification.ComplianceStatus = tawheedResult.ComplianceStatus?.ToUpperInvariant() switch
            {
                "VERIFIED" => ComplianceStatus.Verified,
                "NON-COMPLIANT" or "REJECTED" => ComplianceStatus.NonCompliant,
                "HOLD" => ComplianceStatus.Hold,
                "MANUAL_REVIEW" or "REQUIRES_REVIEW" => ComplianceStatus.ManualReview,
                _ => ComplianceStatus.Unverified,
            };
            verification.ReasonCodes = tawheedResult.ReasonCodes;
            verification.MissingEvidence = tawheedResult.MissingEvidence;
            verification.RequiresHumanReview = tawheedResult.RequiresHumanReview;
            verification.VerifiedAt = DateTimeOffset.UtcNow;

            // Store agent evidence results
            foreach (var agentResult in tawheedResult.AgentResults)
            {
                db.VerificationEvidence.Add(new VerificationEvidence
                {
                    VerificationId = verification.Id,
                    EvidenceType = agentResult.AgentType,
                    Source = $"Tawheed {agentResult.AgentType} agent",
                    Confidence = agentResult.Confidence,
                });
            }

            // Audit trail
            db.VerificationAudits.Add(new VerificationAudit
            {
                VerificationId = verification.Id,
                Action = "TAWHEED_VERIFICATION_COMPLETED",
                Actor = "system",
                Notes = $"Status: {verification.ComplianceStatus}, RiskScore: {tawheedResult.RiskScore:F2}",
            });

            // Update certificate status if verification passed
            if (latestCert is not null && verification.ComplianceStatus == ComplianceStatus.Verified)
            {
                latestCert.Status = CertificateStatus.Verified;
            }

            // Publish completion event
            await eventBus.PublishAsync(new HalalVerificationCompletedEvent(
                productId, verification.Id,
                verification.ComplianceStatus.ToString(),
                tawheedResult.RequiresHumanReview), ct);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            // Tawheed call failed — verification request is accepted but pending
            // processing. Status remains Unverified (not ManualReview) since no
            // automated analysis has completed yet.
            verification.ComplianceStatus = ComplianceStatus.Unverified;
            verification.RequiresHumanReview = false;
            verification.MissingEvidence = ["tawheed_connection_failed"];

            db.VerificationAudits.Add(new VerificationAudit
            {
                VerificationId = verification.Id,
                Action = "TAWHEED_VERIFICATION_FAILED",
                Actor = "system",
                Notes = tawheedResult.ErrorMessage,
            });

            await db.SaveChangesAsync(ct);
        }

        return Accepted(new VerificationDto(verification.Id, verification.ProductId,
            (Contracts.Halal.Dto.ComplianceStatus)verification.ComplianceStatus, verification.PolicyVersion, verification.Jurisdiction,
            verification.ReasonCodes, verification.MissingEvidence,
            verification.RequiresHumanReview, verification.VerifiedAt));
    }

    [HttpGet("products/{productId:guid}/verification")]
    [AllowAnonymous]
    public async Task<ActionResult<VerificationDto>> GetVerification(Guid productId, CancellationToken ct)
    {
        var v = await db.HalalVerifications
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.VerifiedAt)
            .FirstOrDefaultAsync(ct);

        return v is null
            ? NotFound(new ErrorResponse("VERIFICATION_NOT_FOUND", $"No verification found for product {productId}."))
            : Ok(new VerificationDto(v.Id, v.ProductId, (Contracts.Halal.Dto.ComplianceStatus)v.ComplianceStatus, v.PolicyVersion,

                v.Jurisdiction, v.ReasonCodes, v.MissingEvidence, v.RequiresHumanReview, v.VerifiedAt));
    }

    [HttpGet("products/{productId:guid}/audit")]
    [Authorize(Roles = $"{AuthConstants.RoleAdmin},{AuthConstants.RoleVerificationOfficer}")]
    public async Task<ActionResult<VerificationAuditDto[]>> GetAuditTrail(Guid productId, CancellationToken ct)
    {
        var audits = await db.VerificationAudits
            .Where(a => db.HalalVerifications.Any(v => v.Id == a.VerificationId && v.ProductId == productId))
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => new VerificationAuditDto(a.Id, a.VerificationId, a.Action, a.Actor, a.Notes, a.OccurredAt))
            .ToArrayAsync(ct);
        return Ok(audits);
    }

    [HttpPost("products/{productId:guid}/verification/{verificationId:guid}/decision")]
    [Authorize(Roles = AuthConstants.RoleVerificationOfficer)]
    public async Task<ActionResult> RecordDecision(
        Guid productId, Guid verificationId,
        [FromBody] RecordVerificationDecisionRequest request, CancellationToken ct)
    {
        var v = await db.HalalVerifications.FindAsync([verificationId], ct);
        if (v is null) return NotFound(new ErrorResponse("VERIFICATION_NOT_FOUND", "Verification not found."));

        v.ComplianceStatus = request.Decision.ToUpperInvariant() switch
        {
            "VERIFIED" => ComplianceStatus.Verified,
            "REJECTED" => ComplianceStatus.NonCompliant,
            "HOLD" => ComplianceStatus.Hold,
            _ => ComplianceStatus.ManualReview,
        };

        db.VerificationAudits.Add(new VerificationAudit
        {
            VerificationId = verificationId,
            Action = "DECISION_RECORDED",
            Actor = User.Identity?.Name ?? "unknown",
            Notes = request.Notes,
        });

        await eventBus.PublishAsync(new HalalVerificationCompletedEvent(
            productId, verificationId,
            v.ComplianceStatus.ToString(),
            v.RequiresHumanReview), ct);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
