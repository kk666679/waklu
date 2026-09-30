using HalalChain.Domain.Catalog;
using HalalChain.Marketplace.Data;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Marketplace.Repositories;

/// <summary>
/// Implementation of IBrandRepository using EF Core against PlatformDbContext.
/// Provides data access for Brand entities.
/// </summary>
public class BrandRepository : IBrandRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<BrandRepository> _logger;

    public BrandRepository(PlatformDbContext context, ILogger<BrandRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving brand by ID: {BrandId}", id);
            throw;
        }
    }

    public async Task<Brand?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            return await _context.Brands
                .FirstOrDefaultAsync(b => b.Slug == slug, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving brand by slug: {Slug}", slug);
            throw;
        }
    }

    public async Task<IEnumerable<Brand>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Brands
                .OrderBy(b => b.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all brands");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Brands.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting brands");
            throw;
        }
    }

    public async Task AddAsync(Brand brand, CancellationToken ct = default)
    {
        try
        {
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Brand added: {BrandId} - {BrandName}", brand.Id, brand.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding brand: {BrandId}", brand.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Brand brand, CancellationToken ct = default)
    {
        try
        {
            _context.Brands.Update(brand);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Brand updated: {BrandId} - {BrandName}", brand.Id, brand.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating brand: {BrandId}", brand.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == id, ct);
            
            if (brand != null)
            {
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Brand deleted: {BrandId}", id);
            }
            else
            {
                _logger.LogWarning("Brand not found for deletion: {BrandId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting brand: {BrandId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Brands
                .AnyAsync(b => b.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking brand existence: {BrandId}", id);
            throw;
        }
    }
}
