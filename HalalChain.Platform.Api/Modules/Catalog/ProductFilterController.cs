using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Contracts.Catalog;
using HalalChain.Platform.Contracts.Catalog.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HalalStatus = HalalChain.Domain.Catalog.HalalStatus;
using CommercialChannel = HalalChain.Domain.Catalog.CommercialChannel;

namespace HalalChain.Platform.Api.Modules.Catalog;

/// <summary>
/// Product browse &amp; filter API powered by the redesigned taxonomy
/// and the JSONB HalalProfile. Supports filtering on:
///   - taxonomy path (department / category / subcategory / product type slug)
///   - halal status (Certified, VerifiedByPlatform, MuslimFriendly, ...)
///   - dietary tags (multi-select, flags)
///   - origin country
///   - commercial channel (Retail / Wholesale)
///   - price range
///   - free-text search
/// All halal-attribute filters are translated to JSONB containment queries
/// against the GIN-indexed HalalProfile column.
/// </summary>
[ApiController]
[Route("api/v1/catalog")]
[AllowAnonymous]
[ApiVersion("1.0")]
public sealed class ProductFilterController(HalalChainDbContext db) : ControllerBase
{
    [HttpGet("products/filter")]
    public async Task<ActionResult<PagedResult<EnrichedProductDto>>> FilterProducts(
        [FromQuery] string? path,                  // e.g. "food-beverage/snacks-confectionery/chips/potato"
        [FromQuery] string? type,                  // product-type slug (leaf)
        [FromQuery] string? department,            // department slug
        [FromQuery] string? category,              // category slug
        [FromQuery] string? subcategory,           // subcategory slug
        [FromQuery] int? status,                   // HalalStatus enum int
        [FromQuery] int? dietary,                  // DietaryTags flags int (multi-select via bitwise)
        [FromQuery] string? country,               // ISO-2 origin country
        [FromQuery] int? channel,                  // CommercialChannel enum int
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? search,
        [FromQuery] string? sortBy = "newest",     // "newest" | "price-asc" | "price-desc" | "name"
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var q = db.Products
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Include(p => p.Brand)
            .Include(p => p.ProductType).ThenInclude(t => t!.Subcategory)
                .ThenInclude(s => s.Category).ThenInclude(c => c.Department)
            .AsQueryable();

        // ── Path filters ────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(path))
            q = q.Where(p => p.ProductType != null && p.ProductType.PathSlug.StartsWith(path));
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(p => p.ProductType != null && p.ProductType.Slug == type);
        if (!string.IsNullOrWhiteSpace(subcategory))
            q = q.Where(p => p.ProductType != null && p.ProductType.Subcategory.Slug == subcategory);
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(p => p.ProductType != null && p.ProductType.Subcategory.Category.Slug == category);
        if (!string.IsNullOrWhiteSpace(department))
            q = q.Where(p => p.ProductType != null && p.ProductType.Subcategory.Category.Department.Slug == department);

        // ── HalalProfile JSONB filters ─────────────────────────────
        // The HalalProfile is nullable (legacy products + products without
        // a profile yet). For status/dietary/country/channel filters we
        // require a non-null profile so the comparison is meaningful; rows
        // with a null profile simply don't match these filters.
        if (status.HasValue)
            q = q.Where(p => p.HalalProfile != null && p.HalalProfile.Status == (HalalStatus)status.Value);

        if (dietary.HasValue && dietary.Value != 0)
        {
            // bitwise AND — match if the product carries ALL of the requested dietary flags
            var mask = dietary.Value;
            q = q.Where(p => p.HalalProfile != null && ((int)p.HalalProfile.Dietary & mask) == mask);
        }

        if (!string.IsNullOrWhiteSpace(country))
            q = q.Where(p => p.HalalProfile != null && (p.HalalProfile.CountryOfOrigin == country || p.HalalProfile.CountryOfManufacture == country));

        if (channel.HasValue)
            q = q.Where(p => p.HalalProfile != null && p.HalalProfile.Channel == (CommercialChannel)channel.Value);

        // ── Price & text search ────────────────────────────────────
        if (minPrice.HasValue) q = q.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) q = q.Where(p => p.Price <= maxPrice.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(p => p.Title.ToLower().Contains(s) || (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        // ── Sort ────────────────────────────────────────────────────
        q = sortBy switch
        {
            "price-asc"  => q.OrderBy(p => p.Price),
            "price-desc" => q.OrderByDescending(p => p.Price),
            "name"       => q.OrderBy(p => p.Title),
            _            => q.OrderByDescending(p => p.CreatedAt),
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var certs = await db.CertificationBodies.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var dtos = items.Select(p => p.ToEnrichedDto(certs)).ToArray();

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new PagedResult<EnrichedProductDto>(dtos, page, pageSize, total, totalPages));
    }
}
