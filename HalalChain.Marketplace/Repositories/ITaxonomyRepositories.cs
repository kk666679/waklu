using HalalChain.Domain.Catalog;

namespace HalalChain.Marketplace.Repositories;

/// <summary>
/// Repository for Department entity queries and persistence.
/// </summary>
public interface IDepartmentRepository
{
    /// <summary>Get a department by ID.</summary>
    Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get a department by its URL slug.</summary>
    Task<Department?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Get all departments.</summary>
    Task<IEnumerable<Department>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Get only active departments.</summary>
    Task<IEnumerable<Department>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Save a new department.</summary>
    Task AddAsync(Department department, CancellationToken ct = default);

    /// <summary>Update an existing department.</summary>
    Task UpdateAsync(Department department, CancellationToken ct = default);

    /// <summary>Delete a department.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a department exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repository for TaxonomyCategory entity queries and persistence.
/// </summary>
public interface ITaxonomyCategoryRepository
{
    /// <summary>Get a taxonomy category by ID with department included.</summary>
    Task<TaxonomyCategory?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get categories by department.</summary>
    Task<IEnumerable<TaxonomyCategory>> GetByDepartmentAsync(Guid departmentId, CancellationToken ct = default);

    /// <summary>Get a category by slug within a specific department.</summary>
    Task<TaxonomyCategory?> GetBySlugAsync(string slug, Guid departmentId, CancellationToken ct = default);

    /// <summary>Get all taxonomy categories.</summary>
    Task<IEnumerable<TaxonomyCategory>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Get only active categories.</summary>
    Task<IEnumerable<TaxonomyCategory>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Save a new taxonomy category.</summary>
    Task AddAsync(TaxonomyCategory category, CancellationToken ct = default);

    /// <summary>Update an existing taxonomy category.</summary>
    Task UpdateAsync(TaxonomyCategory category, CancellationToken ct = default);

    /// <summary>Delete a taxonomy category.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a category exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repository for Subcategory entity queries and persistence.
/// </summary>
public interface ISubcategoryRepository
{
    /// <summary>Get a subcategory by ID with category included.</summary>
    Task<Subcategory?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get subcategories by category.</summary>
    Task<IEnumerable<Subcategory>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);

    /// <summary>Get a subcategory by slug within a specific category.</summary>
    Task<Subcategory?> GetBySlugAsync(string slug, Guid categoryId, CancellationToken ct = default);

    /// <summary>Get all subcategories.</summary>
    Task<IEnumerable<Subcategory>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Get only active subcategories.</summary>
    Task<IEnumerable<Subcategory>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Save a new subcategory.</summary>
    Task AddAsync(Subcategory subcategory, CancellationToken ct = default);

    /// <summary>Update an existing subcategory.</summary>
    Task UpdateAsync(Subcategory subcategory, CancellationToken ct = default);

    /// <summary>Delete a subcategory.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a subcategory exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repository for ProductType entity queries and persistence.
/// </summary>
public interface IProductTypeRepository
{
    /// <summary>Get a product type by ID with subcategory included.</summary>
    Task<ProductType?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get product types by subcategory.</summary>
    Task<IEnumerable<ProductType>> GetBySubcategoryAsync(Guid subcategoryId, CancellationToken ct = default);

    /// <summary>Get a product type by slug within a specific subcategory.</summary>
    Task<ProductType?> GetBySlugAsync(string slug, Guid subcategoryId, CancellationToken ct = default);

    /// <summary>Get all product types.</summary>
    Task<IEnumerable<ProductType>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Get only active product types.</summary>
    Task<IEnumerable<ProductType>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Save a new product type.</summary>
    Task AddAsync(ProductType productType, CancellationToken ct = default);

    /// <summary>Update an existing product type.</summary>
    Task UpdateAsync(ProductType productType, CancellationToken ct = default);

    /// <summary>Delete a product type.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a product type exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
