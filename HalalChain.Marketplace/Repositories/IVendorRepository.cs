using HalalChain.Domain.Vendors;

namespace HalalChain.Marketplace.Repositories;

/// <summary>
/// Repository for Vendor aggregate root queries and persistence.
/// Manages vendor/merchant entities and their metadata.
/// </summary>
public interface IVendorRepository
{
    /// <summary>Get a vendor by ID.</summary>
    Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get a vendor by slug.</summary>
    Task<Vendor?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Get vendors by status (Pending, Active, Suspended).</summary>
    Task<IEnumerable<Vendor>> GetByStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get active vendors (for marketplace display).</summary>
    Task<IEnumerable<Vendor>> GetActiveAsync(int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get vendors in a specific country.</summary>
    Task<IEnumerable<Vendor>> GetByCountryAsync(string countryCode, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get all vendors (with pagination).</summary>
    Task<IEnumerable<Vendor>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get vendor count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>Get count of active vendors.</summary>
    Task<int> GetActiveCountAsync(CancellationToken ct = default);

    /// <summary>Save a new vendor.</summary>
    Task AddAsync(Vendor vendor, CancellationToken ct = default);

    /// <summary>Update an existing vendor.</summary>
    Task UpdateAsync(Vendor vendor, CancellationToken ct = default);

    /// <summary>Delete a vendor (soft delete recommended).</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a vendor exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a vendor slug is unique.</summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
}
