using HalalChain.Domain.Catalog;
using HalalChain.Marketplace.Data;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Marketplace.Repositories;

/// <summary>Implementation of IDepartmentRepository using EF Core.</summary>
public class DepartmentRepository : IDepartmentRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<DepartmentRepository> _logger;

    public DepartmentRepository(PlatformDbContext context, ILogger<DepartmentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Departments
                .Include(d => d.Categories)
                .FirstOrDefaultAsync(d => d.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department by ID: {DepartmentId}", id);
            throw;
        }
    }

    public async Task<Department?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            return await _context.Departments
                .Include(d => d.Categories)
                .FirstOrDefaultAsync(d => d.Slug == slug, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department by slug: {Slug}", slug);
            throw;
        }
    }

    public async Task<IEnumerable<Department>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Departments
                .OrderBy(d => d.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all departments");
            throw;
        }
    }

    public async Task<IEnumerable<Department>> GetActiveAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active departments");
            throw;
        }
    }

    public async Task AddAsync(Department department, CancellationToken ct = default)
    {
        try
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Department added: {DepartmentId}", department.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding department: {DepartmentId}", department.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Department department, CancellationToken ct = default)
    {
        try
        {
            _context.Departments.Update(department);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Department updated: {DepartmentId}", department.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department: {DepartmentId}", department.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (dept != null)
            {
                _context.Departments.Remove(dept);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Department deleted: {DepartmentId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department: {DepartmentId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Departments.AnyAsync(d => d.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking department existence: {DepartmentId}", id);
            throw;
        }
    }
}

/// <summary>Implementation of ITaxonomyCategoryRepository using EF Core.</summary>
public class TaxonomyCategoryRepository : ITaxonomyCategoryRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<TaxonomyCategoryRepository> _logger;

    public TaxonomyCategoryRepository(PlatformDbContext context, ILogger<TaxonomyCategoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TaxonomyCategory?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.TaxonomyCategories
                .Include(tc => tc.Department)
                .Include(tc => tc.Subcategories)
                .FirstOrDefaultAsync(tc => tc.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving taxonomy category by ID: {CategoryId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<TaxonomyCategory>> GetByDepartmentAsync(Guid departmentId, CancellationToken ct = default)
    {
        try
        {
            return await _context.TaxonomyCategories
                .Where(tc => tc.DepartmentId == departmentId)
                .OrderBy(tc => tc.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories for department: {DepartmentId}", departmentId);
            throw;
        }
    }

    public async Task<TaxonomyCategory?> GetBySlugAsync(string slug, Guid departmentId, CancellationToken ct = default)
    {
        try
        {
            return await _context.TaxonomyCategories
                .FirstOrDefaultAsync(tc => tc.DepartmentId == departmentId && tc.Slug == slug, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category by slug: {Slug}", slug);
            throw;
        }
    }

    public async Task<IEnumerable<TaxonomyCategory>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.TaxonomyCategories
                .OrderBy(tc => tc.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all taxonomy categories");
            throw;
        }
    }

    public async Task<IEnumerable<TaxonomyCategory>> GetActiveAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.TaxonomyCategories
                .Where(tc => tc.IsActive)
                .OrderBy(tc => tc.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active taxonomy categories");
            throw;
        }
    }

    public async Task AddAsync(TaxonomyCategory category, CancellationToken ct = default)
    {
        try
        {
            _context.TaxonomyCategories.Add(category);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Taxonomy category added: {CategoryId}", category.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding taxonomy category: {CategoryId}", category.Id);
            throw;
        }
    }

    public async Task UpdateAsync(TaxonomyCategory category, CancellationToken ct = default)
    {
        try
        {
            _context.TaxonomyCategories.Update(category);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Taxonomy category updated: {CategoryId}", category.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating taxonomy category: {CategoryId}", category.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var cat = await _context.TaxonomyCategories.FirstOrDefaultAsync(tc => tc.Id == id, ct);
            if (cat != null)
            {
                _context.TaxonomyCategories.Remove(cat);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Taxonomy category deleted: {CategoryId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting taxonomy category: {CategoryId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.TaxonomyCategories.AnyAsync(tc => tc.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking taxonomy category existence: {CategoryId}", id);
            throw;
        }
    }
}

/// <summary>Implementation of ISubcategoryRepository using EF Core.</summary>
public class SubcategoryRepository : ISubcategoryRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<SubcategoryRepository> _logger;

    public SubcategoryRepository(PlatformDbContext context, ILogger<SubcategoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Subcategory?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Subcategories
                .Include(s => s.Category)
                .Include(s => s.ProductTypes)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subcategory by ID: {SubcategoryId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Subcategory>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        try
        {
            return await _context.Subcategories
                .Where(s => s.CategoryId == categoryId)
                .OrderBy(s => s.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subcategories for category: {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<Subcategory?> GetBySlugAsync(string slug, Guid categoryId, CancellationToken ct = default)
    {
        try
        {
            return await _context.Subcategories
                .FirstOrDefaultAsync(s => s.CategoryId == categoryId && s.Slug == slug, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subcategory by slug: {Slug}", slug);
            throw;
        }
    }

    public async Task<IEnumerable<Subcategory>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Subcategories
                .OrderBy(s => s.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all subcategories");
            throw;
        }
    }

    public async Task<IEnumerable<Subcategory>> GetActiveAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Subcategories
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active subcategories");
            throw;
        }
    }

    public async Task AddAsync(Subcategory subcategory, CancellationToken ct = default)
    {
        try
        {
            _context.Subcategories.Add(subcategory);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Subcategory added: {SubcategoryId}", subcategory.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding subcategory: {SubcategoryId}", subcategory.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Subcategory subcategory, CancellationToken ct = default)
    {
        try
        {
            _context.Subcategories.Update(subcategory);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Subcategory updated: {SubcategoryId}", subcategory.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subcategory: {SubcategoryId}", subcategory.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var subcat = await _context.Subcategories.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (subcat != null)
            {
                _context.Subcategories.Remove(subcat);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Subcategory deleted: {SubcategoryId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subcategory: {SubcategoryId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Subcategories.AnyAsync(s => s.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subcategory existence: {SubcategoryId}", id);
            throw;
        }
    }
}

/// <summary>Implementation of IProductTypeRepository using EF Core.</summary>
public class ProductTypeRepository : IProductTypeRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<ProductTypeRepository> _logger;

    public ProductTypeRepository(PlatformDbContext context, ILogger<ProductTypeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductType?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductTypes
                .Include(pt => pt.Subcategory)
                .FirstOrDefaultAsync(pt => pt.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product type by ID: {ProductTypeId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<ProductType>> GetBySubcategoryAsync(Guid subcategoryId, CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductTypes
                .Where(pt => pt.SubcategoryId == subcategoryId)
                .OrderBy(pt => pt.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product types for subcategory: {SubcategoryId}", subcategoryId);
            throw;
        }
    }

    public async Task<ProductType?> GetBySlugAsync(string slug, Guid subcategoryId, CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductTypes
                .FirstOrDefaultAsync(pt => pt.SubcategoryId == subcategoryId && pt.Slug == slug, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product type by slug: {Slug}", slug);
            throw;
        }
    }

    public async Task<IEnumerable<ProductType>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductTypes
                .OrderBy(pt => pt.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all product types");
            throw;
        }
    }

    public async Task<IEnumerable<ProductType>> GetActiveAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductTypes
                .Where(pt => pt.IsActive)
                .OrderBy(pt => pt.SortOrder)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active product types");
            throw;
        }
    }

    public async Task AddAsync(ProductType productType, CancellationToken ct = default)
    {
        try
        {
            _context.ProductTypes.Add(productType);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Product type added: {ProductTypeId}", productType.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product type: {ProductTypeId}", productType.Id);
            throw;
        }
    }

    public async Task UpdateAsync(ProductType productType, CancellationToken ct = default)
    {
        try
        {
            _context.ProductTypes.Update(productType);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Product type updated: {ProductTypeId}", productType.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product type: {ProductTypeId}", productType.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var pt = await _context.ProductTypes.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (pt != null)
            {
                _context.ProductTypes.Remove(pt);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Product type deleted: {ProductTypeId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product type: {ProductTypeId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.ProductTypes.AnyAsync(p => p.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking product type existence: {ProductTypeId}", id);
            throw;
        }
    }
}
