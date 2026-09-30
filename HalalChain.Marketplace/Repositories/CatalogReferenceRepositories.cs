using HalalChain.Domain.Catalog;
using HalalChain.Marketplace.Data;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Marketplace.Repositories;

/// <summary>Implementation of IProductAttributeRepository using EF Core.</summary>
public class ProductAttributeRepository : IProductAttributeRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<ProductAttributeRepository> _logger;

    public ProductAttributeRepository(PlatformDbContext context, ILogger<ProductAttributeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductAttribute?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductAttributes.FirstOrDefaultAsync(a => a.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attribute by ID: {AttributeId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<ProductAttribute>> GetFilterableAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductAttributes
                .Where(a => a.IsFilterable)
                .OrderBy(a => a.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filterable attributes");
            throw;
        }
    }

    public async Task<IEnumerable<ProductAttribute>> GetVariantAttributesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductAttributes
                .Where(a => a.IsVariantAttribute)
                .OrderBy(a => a.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving variant attributes");
            throw;
        }
    }

    public async Task<IEnumerable<ProductAttribute>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductAttributes
                .OrderBy(a => a.SortOrder)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all attributes");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductAttributes.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting attributes");
            throw;
        }
    }

    public async Task AddAsync(ProductAttribute attribute, CancellationToken ct = default)
    {
        try
        {
            _context.ProductAttributes.Add(attribute);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Attribute added: {AttributeId}", attribute.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding attribute: {AttributeId}", attribute.Id);
            throw;
        }
    }

    public async Task UpdateAsync(ProductAttribute attribute, CancellationToken ct = default)
    {
        try
        {
            _context.ProductAttributes.Update(attribute);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Attribute updated: {AttributeId}", attribute.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attribute: {AttributeId}", attribute.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var attr = await _context.ProductAttributes.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (attr != null)
            {
                _context.ProductAttributes.Remove(attr);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Attribute deleted: {AttributeId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attribute: {AttributeId}", id);
            throw;
        }
    }
}

/// <summary>Implementation of ICertificationBodyRepository using EF Core.</summary>
public class CertificationBodyRepository : ICertificationBodyRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<CertificationBodyRepository> _logger;

    public CertificationBodyRepository(PlatformDbContext context, ILogger<CertificationBodyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CertificationBody?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.CertificationBodies.FirstOrDefaultAsync(c => c.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving certification body by ID: {CertificationBodyId}", id);
            throw;
        }
    }

    public async Task<CertificationBody?> GetByAcronymAsync(string acronym, CancellationToken ct = default)
    {
        try
        {
            return await _context.CertificationBodies.FirstOrDefaultAsync(c => c.Acronym == acronym, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving certification body by acronym: {Acronym}", acronym);
            throw;
        }
    }

    public async Task<IEnumerable<CertificationBody>> GetActiveAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.CertificationBodies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active certification bodies");
            throw;
        }
    }

    public async Task<IEnumerable<CertificationBody>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.CertificationBodies
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all certification bodies");
            throw;
        }
    }

    public async Task<IEnumerable<CertificationBody>> GetByCountryAsync(string country, CancellationToken ct = default)
    {
        try
        {
            return await _context.CertificationBodies
                .Where(c => c.Country == country)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving certification bodies by country: {Country}", country);
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.CertificationBodies.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting certification bodies");
            throw;
        }
    }
}

/// <summary>Implementation of IFacilityRepository using EF Core.</summary>
public class FacilityRepository : IFacilityRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<FacilityRepository> _logger;

    public FacilityRepository(PlatformDbContext context, ILogger<FacilityRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Facility?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Facilities.FirstOrDefaultAsync(f => f.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving facility by ID: {FacilityId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Facility>> GetByVendorAsync(Guid vendorId, CancellationToken ct = default)
    {
        try
        {
            return await _context.Facilities
                .Where(f => f.VendorId == vendorId)
                .OrderBy(f => f.Name)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving facilities for vendor: {VendorId}", vendorId);
            throw;
        }
    }

    public async Task<IEnumerable<Facility>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default)
    {
        try
        {
            return await _context.Facilities
                .OrderBy(f => f.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all facilities");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Facilities.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting facilities");
            throw;
        }
    }
}

/// <summary>Implementation of ICountryRepository using EF Core.</summary>
public class CountryRepository : ICountryRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<CountryRepository> _logger;

    public CountryRepository(PlatformDbContext context, ILogger<CountryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Country?> GetByIso2Async(string iso2, CancellationToken ct = default)
    {
        try
        {
            return await _context.Countries.FirstOrDefaultAsync(c => c.Iso2 == iso2, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving country by ISO2: {Iso2}", iso2);
            throw;
        }
    }

    public async Task<IEnumerable<Country>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Countries
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all countries");
            throw;
        }
    }

    public async Task<IEnumerable<Country>> GetByRegionAsync(string region, CancellationToken ct = default)
    {
        try
        {
            return await _context.Countries
                .Where(c => c.Region == region)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving countries by region: {Region}", region);
            throw;
        }
    }
}
