using HalalChain.Domain.Catalog;
using HalalChain.Marketplace.Data;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Marketplace.Repositories;

/// <summary>Implementation of IProductRepository using EF Core against PlatformDbContext.</summary>
public class ProductRepository : IProductRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(PlatformDbContext context, ILogger<ProductRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .Include(p => p.ProductType)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.MediaAssets)
                .Include(p => p.Attributes)
                .Include(p => p.Certificates)
                .Include(p => p.Verifications)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by ID: {ProductId}", id);
            throw;
        }
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.MediaAssets)
                .FirstOrDefaultAsync(p => p.Slug == slug, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by slug: {Slug}", slug);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> SearchAsync(string query, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        try
        {
            var searchTerm = query.ToLower();
            return await _context.Products
                .Where(p => p.Title.ToLower().Contains(searchTerm) ||
                           p.Description!.ToLower().Contains(searchTerm) ||
                           p.Keywords!.ToLower().Contains(searchTerm))
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products: {Query}", query);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetByVendorAsync(Guid vendorId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Where(p => p.VendorId == vendorId)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for vendor: {VendorId}", vendorId);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by category: {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetByDepartmentAsync(Guid departmentId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by department: {DepartmentId}", departmentId);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetByHalalStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by halal status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetByDietaryTagAsync(string tag, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by dietary tag: {Tag}", tag);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetFeaturedAsync(int take = 12, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Where(p => p.IsSearchIndexed && p.RecommendationReason != null)
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .Include(p => p.MediaAssets)
                .OrderByDescending(p => p.AverageRating)
                .ThenByDescending(p => p.UpdatedAt)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving featured products");
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetByBrandAsync(Guid brandId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Where(p => p.BrandId == brandId)
                .Include(p => p.Vendor)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by brand: {BrandId}", brandId);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetRecentAsync(int take = 20, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .Include(p => p.MediaAssets)
                .OrderByDescending(p => p.CreatedAt)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent products");
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all products");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Products.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting products");
            throw;
        }
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        try
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Product added: {ProductId}", product.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product: {ProductId}", product.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        try
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Product updated: {ProductId}", product.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product: {ProductId}", product.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Product deleted: {ProductId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product: {ProductId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Products.AnyAsync(p => p.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking product existence: {ProductId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        try
        {
            var idList = ids.ToList();
            return await _context.Products
                .Where(p => idList.Contains(p.Id))
                .Include(p => p.Vendor)
                .Include(p => p.Brand)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by IDs");
            throw;
        }
    }
}
