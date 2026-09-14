using HalalChain.Application.Common.Exceptions;
using HalalChain.Application.Common.Interfaces;
using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Catalog.Requests;
using HalalChain.Platform.Contracts.Halal.Dto;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Catalog;

public sealed class EfProductRepository(HalalChainDbContext db) : IProductRepository
{
    public async Task<ProductDto> CreateAsync(CreateProductRequest request, Guid vendorId, CancellationToken ct = default)
    {
        var product = new Domain.Catalog.Product
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = request.Title.Trim().ToLowerInvariant().Replace(' ', '-'),
            Description = request.Description,
            CategoryId = request.CategoryId,
            VendorId = vendorId,
            Origin = request.Origin,
            Price = request.Price,
            Currency = request.Currency,
            Inventory = request.Inventory
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        await db.Entry(product).Reference(p => p.Category).LoadAsync(ct);
        await db.Entry(product).Reference(p => p.Vendor).LoadAsync(ct);
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, Guid vendorId, bool isAdmin, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .Include(p => p.Certificates)
            .SingleOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Product", id);

        if (!isAdmin && product.VendorId != vendorId)
            throw new ForbiddenException("The product does not belong to the current vendor.");

        if (request.Title is not null)
        {
            product.Title = request.Title;
            product.Slug = request.Title.Trim().ToLowerInvariant().Replace(' ', '-');
        }

        if (request.Description is not null) product.Description = request.Description;
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.Inventory.HasValue) product.Inventory = request.Inventory.Value;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([id], ct)
            ?? throw new NotFoundException("Product", id);
        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .Include(p => p.Certificates)
            .SingleOrDefaultAsync(p => p.Id == id, ct);
        return product is null ? null : ToDto(product);
    }

    public async Task<PagedResult<ProductDto>> ListAsync(
        string? category, string? halalStatus, decimal? minPrice, decimal? maxPrice,
        string? vendor, string? search, string? sortBy, string? sortOrder,
        int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .Include(p => p.Certificates)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category != null && (p.Category.Slug == category || p.Category.Name == category));
        if (!string.IsNullOrWhiteSpace(vendor))
            query = query.Where(p => p.Vendor.Slug == vendor || p.Vendor.Name == vendor);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(p => p.Title.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
        }
        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);
        if (!string.IsNullOrWhiteSpace(halalStatus))
        {
            var status = halalStatus.ToLowerInvariant();
            query = status switch
            {
                "verified" => query.Where(p => p.Certificates.Any(c => c.Status == CertificateStatus.Verified && c.ExpiryDate > DateTimeOffset.UtcNow)),
                "unverified" => query.Where(p => !p.Certificates.Any(c => c.Status == CertificateStatus.Verified && c.ExpiryDate > DateTimeOffset.UtcNow)),
                "expired" => query.Where(p => p.Certificates.Any(c => c.Status == CertificateStatus.Verified && c.ExpiryDate <= DateTimeOffset.UtcNow)),
                "pending" => query.Where(p => p.Certificates.Any(c => c.Status == CertificateStatus.Submitted || c.Status == CertificateStatus.DocumentReview || c.Status == CertificateStatus.Verification)),
                _ => query
            };
        }

        var totalCount = await query.CountAsync(ct);
        query = sortBy?.ToLowerInvariant() switch
        {
            "price" => sortOrder == "desc" ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "date" => sortOrder == "desc" ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "inventory" => sortOrder == "desc" ? query.OrderByDescending(p => p.Inventory) : query.OrderBy(p => p.Inventory),
            _ => query.OrderBy(p => p.Title)
        };

        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new PagedResult<ProductDto>(products.Select(ToDto).ToArray(), page, pageSize, totalCount, (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<EnrichedProductDto?> GetEnrichedAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products.AsNoTracking().WithEnrichmentIncludes().SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return null;
        var certBodies = await db.GetCertificationBodyNamesAsync(ct);
        return product.ToEnrichedDto(certBodies);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) => db.Products.AnyAsync(p => p.Id == id, ct);

    public Task<bool> IsOwnedByVendorAsync(Guid productId, Guid vendorId, CancellationToken ct = default) =>
        db.Products.AnyAsync(p => p.Id == productId && p.VendorId == vendorId, ct);

    private static ProductDto ToDto(Domain.Catalog.Product product)
    {
        var verified = product.Certificates.FirstOrDefault(c => c.Status == CertificateStatus.Verified && c.ExpiryDate > DateTimeOffset.UtcNow);
        var status = verified is not null
            ? new HalalStatusDto("Verified", verified.CertificateNumber, verified.CertificationBody, verified.ExpiryDate)
            : product.Certificates.Any(c => c.Status == CertificateStatus.Verified)
                ? new HalalStatusDto("Expired", null, null, null)
                : new HalalStatusDto("Unverified", null, null, null);

        return new ProductDto(product.Id, product.Title, product.Slug, product.Description,
            product.Category?.Name ?? string.Empty, product.Vendor?.Name ?? string.Empty,
            product.Origin, product.Price, product.Currency, product.Inventory, status, product.CreatedAt);
    }
}
