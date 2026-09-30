using HalalChain.Domain.Vendors;
using HalalChain.Marketplace.Data;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Marketplace.Repositories;

/// <summary>Implementation of IVendorRepository using EF Core against PlatformDbContext.</summary>
public class VendorRepository : IVendorRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<VendorRepository> _logger;

    public VendorRepository(PlatformDbContext context, ILogger<VendorRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor by ID: {VendorId}", id);
            throw;
        }
    }

    public async Task<Vendor?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            return await _context.Vendors.FirstOrDefaultAsync(v => v.Slug == slug, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor by slug: {Slug}", slug);
            throw;
        }
    }

    public async Task<IEnumerable<Vendor>> GetByStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Vendors
                .Where(v => v.Status == status)
                .OrderBy(v => v.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendors by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<Vendor>> GetActiveAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Vendors
                .Where(v => v.Status == "Active")
                .OrderBy(v => v.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active vendors");
            throw;
        }
    }

    public async Task<IEnumerable<Vendor>> GetByCountryAsync(string countryCode, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Vendors
                .Where(v => v.Country == countryCode)
                .OrderBy(v => v.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendors by country: {CountryCode}", countryCode);
            throw;
        }
    }

    public async Task<IEnumerable<Vendor>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Vendors
                .OrderBy(v => v.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all vendors");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Vendors.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting vendors");
            throw;
        }
    }

    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Vendors.CountAsync(v => v.Status == "Active", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting active vendors");
            throw;
        }
    }

    public async Task AddAsync(Vendor vendor, CancellationToken ct = default)
    {
        try
        {
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Vendor added: {VendorId}", vendor.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding vendor: {VendorId}", vendor.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Vendor vendor, CancellationToken ct = default)
    {
        try
        {
            _context.Vendors.Update(vendor);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Vendor updated: {VendorId}", vendor.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor: {VendorId}", vendor.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id, ct);
            if (vendor != null)
            {
                _context.Vendors.Remove(vendor);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Vendor deleted: {VendorId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vendor: {VendorId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Vendors.AnyAsync(v => v.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking vendor existence: {VendorId}", id);
            throw;
        }
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
    {
        try
        {
            var query = _context.Vendors.Where(v => v.Slug == slug);
            if (excludeId.HasValue)
            {
                query = query.Where(v => v.Id != excludeId.Value);
            }
            return await query.AnyAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking vendor slug: {Slug}", slug);
            throw;
        }
    }
}
