using HalalChain.Platform.Api.Persistence;
using HalalChain.Application.Common.Abstractions;
using HalalChain.Platform.Contracts.Api.Errors;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Commerce.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Commerce;

/// <summary>
/// Customer wishlist API. Persists liked products per customer so the
/// wishlist survives across sessions (the front-end currently keeps an
/// in-memory copy that is lost on refresh).
/// </summary>
[ApiController]
[Route("api/v1/wishlist")]
[Authorize(Roles = AuthConstants.RoleMarketplaceUser)]
[ApiVersion("1.0")]
public sealed class WishlistController(HalalChainDbContext db, ICurrentUser user) : ControllerBase
{
    private Guid CustomerId => user.RequireUserId();

    [HttpGet]
    public async Task<ActionResult<WishlistDto>> GetWishlist(CancellationToken ct)
    {
        var items = await db.WishlistItems
            .Where(w => w.CustomerId == CustomerId)
            .Include(w => w.Product)
                .ThenInclude(p => p.Vendor)
            .Include(w => w.Product)
                .ThenInclude(p => p.Certificates)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

        var itemsDto = items.Select(w => new WishlistItemDto(
            w.ProductId,
            w.Product!.Title,
            w.Product.Vendor?.Name ?? "",
            w.Product.Price,
            w.Product.Currency,
            w.Product.Certificates?.Any(c => c.Status == CertificateStatus.Verified && c.ExpiryDate > DateTimeOffset.UtcNow) == true
                ? new HalalStatusDto("Verified", null, null, null)
                : w.Product.Certificates?.Any(c => c.Status == CertificateStatus.Verified) == true
                    ? new HalalStatusDto("Expired", null, null, null)
                    : new HalalStatusDto("Unverified", null, null, null),
            w.CreatedAt)).ToArray();

        return Ok(new WishlistDto(CustomerId, itemsDto));
    }

    [HttpPost("{productId:guid}")]
    public async Task<ActionResult<WishlistDto>> AddToWishlist(Guid productId, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([productId], ct);
        if (product is null)
            return NotFound(new ErrorResponse("PRODUCT_NOT_FOUND", $"Product {productId} not found."));

        var existing = await db.WishlistItems
            .FirstOrDefaultAsync(w => w.CustomerId == CustomerId && w.ProductId == productId, ct);

        if (existing is null)
        {
            db.WishlistItems.Add(new WishlistItem
            {
                CustomerId = CustomerId,
                ProductId = productId,
            });
            await db.SaveChangesAsync(ct);
        }

        return await GetWishlist(ct);
    }

    [HttpDelete("{productId:guid}")]
    public async Task<ActionResult<WishlistDto>> RemoveFromWishlist(Guid productId, CancellationToken ct)
    {
        var item = await db.WishlistItems
            .FirstOrDefaultAsync(w => w.CustomerId == CustomerId && w.ProductId == productId, ct);

        if (item is not null)
        {
            db.WishlistItems.Remove(item);
            await db.SaveChangesAsync(ct);
        }

        return await GetWishlist(ct);
    }
}
