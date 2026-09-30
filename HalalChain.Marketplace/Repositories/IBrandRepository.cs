using HalalChain.Domain.Catalog;

namespace HalalChain.Marketplace.Repositories;

/// <summary>
/// Repository for Brand entity queries and persistence.
/// Encapsulates all data access for brands.
/// </summary>
public interface IBrandRepository
{
    /// <summary>Get a brand by ID.</summary>
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get a brand by its URL slug.</summary>
    Task<Brand?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Get all brands with pagination.</summary>
    Task<IEnumerable<Brand>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get brand count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>Save a new brand.</summary>
    Task AddAsync(Brand brand, CancellationToken ct = default);

    /// <summary>Update an existing brand.</summary>
    Task UpdateAsync(Brand brand, CancellationToken ct = default);

    /// <summary>Delete a brand.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a brand exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
