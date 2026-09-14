using HalalChain.Platform.Api.Modules.Blockchain;
using HalalChain.Platform.Api.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Verification;

/// <summary>
/// Public, unauthenticated consumer verification endpoint. Powers the
/// QR code verification page. Returns the trust signal by reading
/// directly from the chain (so consumers can independently verify) and
/// augments with our indexer cache for human-friendly fields (logo,
/// jurisdiction name, last-synced timestamp).
///
/// The "source" field in the response tells the consumer whether the
/// data was last confirmed on-chain or is from our cache; the page
/// must display a banner if the cache is stale (&gt; 5 min) or the
/// chain is unreachable.
/// </summary>
[ApiController]
[Route("api/v1/verify")]
[AllowAnonymous]
[ApiVersion("1.0")]
public sealed class VerificationController : ControllerBase
{
    private readonly ISmartContractService _chain;
    private readonly HalalChainDbContext _db;
    private readonly ILogger<VerificationController> _logger;

    public VerificationController(ISmartContractService chain, HalalChainDbContext db, ILogger<VerificationController> logger)
    {
        _chain = chain; _db = db; _logger = logger;
    }

    /// <summary>Resolve a productId to its on-chain + off-chain verification view.</summary>
    [HttpGet("{productId}")]
    public async Task<ActionResult<VerificationView>> Verify(string productId, CancellationToken ct)
    {
        // 1. Trust signal: read the product + current cert directly from the chain.
        var onChainProduct = await _chain.GetProductAsync(productId, ct);
        if (onChainProduct.Source == "chain" && onChainProduct.Value is null)
            return NotFound(new { error = "Product not found on chain", productId });
        if (onChainProduct.Value is null)
            return StatusCode(503, new { error = "Chain unreachable and no cached data", productId });

        // 2. Read the current cert (if any)
        CertificateOnChainDto? cert = null;
        if (!string.IsNullOrEmpty(onChainProduct.Value.CurrentCertId))
            cert = (await _chain.GetCertificateAsync(onChainProduct.Value.CurrentCertId, ct)).Value;

        // 3. Augment with indexer cache (off-chain fields like last-synced, friendly names)
        var cachedProduct = await _db.ProductsOnChain.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        // 4. Pull supplier from indexer (for the friendly name; PII is never on-chain)
        var cachedSupplier = cachedProduct is null ? null : await _db.SuppliersOnChain.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SupplierId == cachedProduct.SupplierId, ct);

        // 5. Build the response
        var status = (ProductStatus)onChainProduct.Value.Status;
        var certStatus = cert is null ? (CertificateStatus?)null : (CertificateStatus)cert.Status;
        var now = DateTimeOffset.UtcNow;
        var expired = cert is not null && cert.ExpiresAt < now;
        var warning = new List<string>();
        if (status == ProductStatus.Recalled) warning.Add("Product RECALLED");
        if (expired) warning.Add("Certificate expired");
        if (onChainProduct.Source == "fallback") warning.Add("Chain unreachable, showing last-known state");
        if (cachedProduct is not null && (DateTimeOffset.UtcNow - cachedProduct.IndexedAt) > TimeSpan.FromMinutes(5))
            warning.Add("Cache may be stale");

        return Ok(new VerificationView(
            productId,
            onChainProduct.Source,
            new VerificationProduct(
                onChainProduct.Value.SupplierId,
                onChainProduct.Value.IpfsCid,
                onChainProduct.Value.Jurisdiction,
                onChainProduct.Value.CurrentCertId,
                status.ToString(),
                onChainProduct.Value.RegisteredAt.ToUnixTimeSeconds(),
                onChainProduct.Value.UpdatedAt.ToUnixTimeSeconds()),
            cert is null ? null : new VerificationCertificate(
                cert.CertId,
                cert.Certifier,
                cert.Country,
                cert.DocumentCid,
                cert.IssuedAt.ToUnixTimeSeconds(),
                cert.ExpiresAt.ToUnixTimeSeconds(),
                expired ? "Expired" : certStatus?.ToString() ?? "Unknown"),
            cachedSupplier is null ? null : new VerificationSupplier(
                cachedSupplier.SupplierId,
                cachedSupplier.Wallet,
                cachedSupplier.Jurisdiction,
                ((SupplierStatus)cachedSupplier.Status).ToString()),
            warning,
            DateTimeOffset.UtcNow));
    }

    public enum ProductStatus { Pending = 0, Verified = 1, Suspended = 2, Recalled = 3 }
    public enum SupplierStatus { Active = 0, Suspended = 1, Revoked = 2 }
    public enum CertificateStatus { Active = 0, Revoked = 1, Expired = 2 }
}

public sealed record VerificationView(
    string ProductId,
    string Source,
    VerificationProduct Product,
    VerificationCertificate? Certificate,
    VerificationSupplier? Supplier,
    IReadOnlyList<string> Warnings,
    DateTimeOffset VerifiedAt);

public sealed record VerificationProduct(
    string SupplierId,
    string IpfsCid,
    string Jurisdiction,
    string CurrentCertId,
    string Status,
    long RegisteredAtUnix,
    long UpdatedAtUnix);

public sealed record VerificationCertificate(
    string CertId,
    string Certifier,
    string Country,
    string DocumentCid,
    long IssuedAtUnix,
    long ExpiresAtUnix,
    string Status);

public sealed record VerificationSupplier(
    string SupplierId,
    string Wallet,
    string Jurisdiction,
    string Status);
