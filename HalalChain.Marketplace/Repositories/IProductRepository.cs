using HalalChain.Domain.Catalog;

namespace HalalChain.Marketplace.Repositories;

/// <summary>
/// Repository for Product aggregate root queries and persistence.
/// Encapsulates all data access for products, variants, and related catalog data.
/// </summary>
public interface IProductRepository
{
    /// <summary>Get a product by ID with all related data (variants, media, attributes, halal profile).</summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get a product by its URL slug.</summary>
    Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Search products by title, description, or keywords.</summary>
    Task<IEnumerable<Product>> SearchAsync(string query, int skip = 0, int take = 20, CancellationToken ct = default);

    /// <summary>Get all products for a specific vendor.</summary>
    Task<IEnumerable<Product>> GetByVendorAsync(Guid vendorId, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get all products in a specific category (4-level taxonomy).</summary>
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get all products in a department (top-level taxonomy).</summary>
    Task<IEnumerable<Product>> GetByDepartmentAsync(Guid departmentId, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get products by halal status (Certified, VerifiedByPlatform, MuslimFriendly, etc.).</summary>
    Task<IEnumerable<Product>> GetByHalalStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get products with specific dietary tags (Vegetarian, Vegan, GlutenFree, etc.).</summary>
    Task<IEnumerable<Product>> GetByDietaryTagAsync(string tag, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get featured/recommended products.</summary>
    Task<IEnumerable<Product>> GetFeaturedAsync(int take = 12, CancellationToken ct = default);

    /// <summary>Get products for a specific brand.</summary>
    Task<IEnumerable<Product>> GetByBrandAsync(Guid brandId, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get recently added/updated products.</summary>
    Task<IEnumerable<Product>> GetRecentAsync(int take = 20, CancellationToken ct = default);

    /// <summary>Get all products (with pagination).</summary>
    Task<IEnumerable<Product>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get product count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>Save a new product.</summary>
    Task AddAsync(Product product, CancellationToken ct = default);

    /// <summary>Update an existing product.</summary>
    Task UpdateAsync(Product product, CancellationToken ct = default);

    /// <summary>Delete a product.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a product exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get products by IDs (for bulk operations).</summary>
    Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
