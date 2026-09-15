using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Api.Modules.Events;
using HalalChain.Platform.Contracts.Api.Errors;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Catalog.Requests;
using HalalChain.Platform.Contracts.Halal.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Catalog;

[ApiController]
[Route("api/v1/catalog")]
[Authorize]
[ApiVersion("1.0")]
public sealed class CatalogController(HalalChainDbContext db, IEventBus eventBus, ICurrentUser user) : ControllerBase
{
    [HttpGet("products")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<ProductDto>>> ListProducts(
        [FromQuery] string? category,
        [FromQuery] string? halalStatus,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? vendor,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var q = db.Products
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .Include(p => p.Certificates)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(p => p.Category != null && (p.Category.Slug == category || p.Category.Name == category));

        if (!string.IsNullOrWhiteSpace(halalStatus))
        {
            var status = halalStatus.ToLowerInvariant();
            q = status switch
            {
                "verified" => q.Where(p => p.Certificates.Any(c => c.Status == CertificateStatus.Verified && c.ExpiryDate > DateTimeOffset.UtcNow)),
                "unverified" => q.Where(p => !p.Certificates.Any(c => c.Status == CertificateStatus.Verified && c.ExpiryDate > DateTimeOffset.UtcNow)),
                "expired" => q.Where(p => p.Certificates.Any(c => c.Status == CertificateStatus.Verified && c.ExpiryDate <= DateTimeOffset.UtcNow)),
                "pending" => q.Where(p => p.Certificates.Any(c => c.Status == CertificateStatus.Submitted || c.Status == CertificateStatus.DocumentReview || c.Status == CertificateStatus.Verification)),
                _ => q
            };
        }

        if (minPrice.HasValue) q = q.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) q = q.Where(p => p.Price <= maxPrice.Value);
        if (!string.IsNullOrWhiteSpace(vendor)) q = q.Where(p => p.Vendor.Slug == vendor || p.Vendor.Name == vendor);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            q = q.Where(p => p.Title.ToLower().Contains(searchLower) || (p.Description != null && p.Description.ToLower().Contains(searchLower)));
        }

        var totalCount = await q.CountAsync(ct);
        q = sortBy?.ToLowerInvariant() switch
        {
            "price" => sortOrder == "desc" ? q.OrderByDescending(p => p.Price) : q.OrderBy(p => p.Price),
            "date" => sortOrder == "desc" ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt),
            "inventory" => sortOrder == "desc" ? q.OrderByDescending(p => p.Inventory) : q.OrderBy(p => p.Inventory),
            _ => q.OrderBy(p => p.Title)
        };

        var products = await q.Skip((page - 1) * pageSize).Take(pageSize).Select(p => p.ToDto()).ToArrayAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new PagedResult<ProductDto>(products, page, pageSize, totalCount, totalPages));
    }

    [HttpGet("products/{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id, CancellationToken ct)
    {
        var p = await db.Products.Include(p => p.Category).Include(p => p.Vendor).Include(p => p.Certificates).FirstOrDefaultAsync(p => p.Id == id, ct);
        return p is null ? NotFound(new ErrorResponse("PRODUCT_NOT_FOUND", $"Product {id} not found.")) : Ok(p.ToDto());
    }

    /// <summary>
    /// Returns the full enriched product view — including the HalalProfile JSONB
    /// (ingredients, allergens, dietary tags, animal derivatives) and the complete
    /// taxonomy path (Department → Category → Subcategory → ProductType).
    /// Used by the marketplace product detail page for the trust & verification
    /// experience.
    /// </summary>
    [HttpGet("products/{id:guid}/enriched")]
    [AllowAnonymous]
    public async Task<ActionResult<EnrichedProductDto>> GetEnrichedProduct(Guid id, CancellationToken ct)
    {
        var certBodies = await db.CertificationBodies.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var p = await db.Products.WithEnrichmentIncludes()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (p is null)
            return NotFound(new ErrorResponse("PRODUCT_NOT_FOUND", $"Product {id} not found."));

        return Ok(p.ToEnrichedDto(certBodies));
    }

    [HttpPost("products")]
    [Authorize(Roles = AuthConstants.RoleVendor)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new ErrorResponse("INVALID_TITLE", "Title is required."));

        var product = new Product
        {
            Id = Guid.NewGuid(), Title = request.Title, Slug = request.Title.ToLowerInvariant().Replace(" ", "-"),
            Description = request.Description, CategoryId = request.CategoryId,
            VendorId = user.RequireUserId(),
            Origin = request.Origin, Price = request.Price, Currency = request.Currency, Inventory = request.Inventory,
        };

        db.Products.Add(product);
        await eventBus.PublishAsync(new ProductCreatedEvent(product.Id, product.Title, product.Slug), ct);
        await db.SaveChangesAsync(ct);

        await db.Entry(product).Reference(p => p.Category).LoadAsync(ct);
        await db.Entry(product).Reference(p => p.Vendor).LoadAsync(ct);
        return Ok(product.ToDto());
    }

    [HttpPatch("products/{id:guid}")]
    [Authorize(Roles = $"{AuthConstants.RoleVendor},{AuthConstants.RoleAdmin}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return NotFound(new ErrorResponse("PRODUCT_NOT_FOUND", $"Product {id} not found."));

        var subjectId = Guid.TryParse(User.Identity?.Name, out var g) ? g : Guid.Empty;
        if (product.VendorId != subjectId && !User.IsInRole(AuthConstants.RoleAdmin)) return Forbid();

        if (request.Title is not null) product.Title = request.Title;
        if (request.Description is not null) product.Description = request.Description;
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.Inventory.HasValue) product.Inventory = request.Inventory.Value;

        await eventBus.PublishAsync(new ProductUpdatedEvent(product.Id, product.Title), ct);
        await db.SaveChangesAsync(ct);

        await db.Entry(product).Reference(p => p.Category).LoadAsync(ct);
        await db.Entry(product).Reference(p => p.Vendor).LoadAsync(ct);
        return Ok(product.ToDto());
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<CategoryDto[]>> ListCategories(CancellationToken ct)
    {
        var cats = await db.Categories.OrderBy(c => c.Name).ToArrayAsync(ct);
        var catsDto = cats.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.ParentId?.ToString(), c.Products.Count)).ToArray();
        return Ok(catsDto);
    }
}

file static class ProductExtensions
{
    public static ProductDto ToDto(this Product p) => new(
        p.Id, p.Title, p.Slug, p.Description,
        p.Category?.Name ?? "", p.Vendor?.Name ?? "", p.Origin,
        p.Price, p.Currency, p.Inventory,
        p.Certificates.FirstOrDefault(c => c.Status == CertificateStatus.Verified && c.ExpiryDate > DateTimeOffset.UtcNow) is { } v
            ? new HalalStatusDto("Verified", v.CertificateNumber, v.CertificationBody, v.ExpiryDate)
            : p.Certificates.Any(c => c.Status == CertificateStatus.Verified)
                ? new HalalStatusDto("Expired", null, null, null)
                : new HalalStatusDto("Unverified", null, null, null),
        p.CreatedAt);
}
