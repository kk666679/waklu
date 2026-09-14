using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Contracts.Catalog;
using HalalChain.Platform.Contracts.Catalog.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DietaryTags = HalalChain.Platform.Contracts.Catalog.DietaryTags;
using HalalStatus = HalalChain.Platform.Contracts.Catalog.HalalStatus;

namespace HalalChain.Platform.Api.Modules.Catalog;

/// <summary>
/// Public catalog taxonomy &amp; filter API. Exposes the redesigned
/// 4-level commerce path (Department → Category → Subcategory → ProductType)
/// and the new product filter endpoint that queries the HalalProfile JSONB.
/// </summary>
[ApiController]
[Route("api/v1/catalog")]
[AllowAnonymous]
[ApiVersion("1.0")]
public sealed class TaxonomyController(HalalChainDbContext db) : ControllerBase
{
    // ── Departments ────────────────────────────────────────────────

    [HttpGet("departments")]
    public async Task<ActionResult<DepartmentDto[]>> ListDepartments(CancellationToken ct)
    {
        var depts = await db.Departments
            .AsNoTracking()
            .OrderBy(d => d.SortOrder)
            .Select(d => new DepartmentDto(
                d.Id, d.Name, d.Slug, d.Description, d.IconClass, d.SortOrder,
                d.Categories.Count(c => c.IsActive),
                db.ProductTypes.Count(t => t.Subcategory!.Category!.DepartmentId == d.Id && t.IsActive)))
            .ToArrayAsync(ct);
        return Ok(depts);
    }

    // ── Categories (within a department) ────────────────────────────

    [HttpGet("departments/{departmentSlug}/categories")]
    public async Task<ActionResult<TaxonomyCategoryDto[]>> ListCategories(
        string departmentSlug, CancellationToken ct)
    {
        var dept = await db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Slug == departmentSlug, ct);
        if (dept is null) return NotFound();

        var cats = await db.TaxonomyCategories
            .AsNoTracking()
            .Where(c => c.DepartmentId == dept.Id && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new TaxonomyCategoryDto(
                c.Id, c.DepartmentId, dept.Slug, c.Name, c.Slug, c.Description, c.IconClass,
                c.SortOrder,
                c.Subcategories.Count(s => s.IsActive),
                db.ProductTypes.Count(t => t.Subcategory!.CategoryId == c.Id && t.IsActive)))
            .ToArrayAsync(ct);
        return Ok(cats);
    }

    // ── Subcategories (within a category) ───────────────────────────

    [HttpGet("categories/{categorySlug}/subcategories")]
    public async Task<ActionResult<SubcategoryDto[]>> ListSubcategories(
        string categorySlug, CancellationToken ct)
    {
        var cat = await db.TaxonomyCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == categorySlug, ct);
        if (cat is null) return NotFound();

        var subs = await db.Subcategories
            .AsNoTracking()
            .Where(s => s.CategoryId == cat.Id && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new SubcategoryDto(
                s.Id, s.CategoryId, cat.Slug, s.Name, s.Slug, s.SortOrder,
                s.ProductTypes.Count(t => t.IsActive),
                db.Products.Count(p => p.ProductType!.SubcategoryId == s.Id)))
            .ToArrayAsync(ct);
        return Ok(subs);
    }

    // ── Product types (the leaf, within a subcategory) ─────────────

    [HttpGet("subcategories/{subcategorySlug}/types")]
    public async Task<ActionResult<ProductTypeDto[]>> ListProductTypes(
        string subcategorySlug, CancellationToken ct)
    {
        var sub = await db.Subcategories.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == subcategorySlug, ct);
        if (sub is null) return NotFound();

        var types = await db.ProductTypes
            .AsNoTracking()
            .Where(t => t.SubcategoryId == sub.Id && t.IsActive)
            .OrderBy(t => t.SortOrder)
            .Select(t => new ProductTypeDto(
                t.Id, t.SubcategoryId, sub.Slug, t.Name, t.Slug, t.PathSlug, t.SortOrder,
                db.Products.Count(p => p.ProductTypeId == t.Id)))
            .ToArrayAsync(ct);
        return Ok(types);
    }

    // ── Resolve a full breadcrumb path ─────────────────────────────

    [HttpGet("path/{**pathSlug}")]
    public async Task<ActionResult<TaxonomyPathDto>> ResolvePath(
        string pathSlug, CancellationToken ct)
    {
        var t = await db.ProductTypes.AsNoTracking()
            .Include(t => t.Subcategory).ThenInclude(s => s.Category).ThenInclude(c => c.Department)
            .FirstOrDefaultAsync(t => t.PathSlug == pathSlug || t.Slug == pathSlug, ct);
        if (t is null) return NotFound();

        var dto = new TaxonomyPathDto(
            t.PathSlug,
            new DepartmentDto(t.Subcategory.Category.Department.Id,
                t.Subcategory.Category.Department.Name,
                t.Subcategory.Category.Department.Slug,
                t.Subcategory.Category.Department.Description,
                t.Subcategory.Category.Department.IconClass,
                t.Subcategory.Category.Department.SortOrder, 0, 0),
            new TaxonomyCategoryDto(t.Subcategory.Category.Id,
                t.Subcategory.Category.DepartmentId,
                t.Subcategory.Category.Department.Slug,
                t.Subcategory.Category.Name,
                t.Subcategory.Category.Slug,
                t.Subcategory.Category.Description,
                t.Subcategory.Category.IconClass,
                t.Subcategory.Category.SortOrder, 0, 0),
            new SubcategoryDto(t.Subcategory.Id, t.Subcategory.CategoryId,
                t.Subcategory.Category.Slug, t.Subcategory.Name, t.Subcategory.Slug,
                t.Subcategory.SortOrder, 0, 0),
            new ProductTypeDto(t.Id, t.SubcategoryId, t.Subcategory.Slug,
                t.Name, t.Slug, t.PathSlug, t.SortOrder, 0));
        return Ok(dto);
    }

    // ── Reference data ─────────────────────────────────────────────

    [HttpGet("certification-bodies")]
    public async Task<ActionResult<CertificationBodyDto[]>> ListCertificationBodies(CancellationToken ct)
    {
        var bodies = await db.CertificationBodies
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Country).ThenBy(b => b.Name)
            .Select(b => new CertificationBodyDto(
                b.Id, b.Name, b.Slug, b.Acronym, b.Country, (int)b.TrustTier, b.LogoUrl, b.Description))
            .ToArrayAsync(ct);
        return Ok(bodies);
    }

    [HttpGet("countries")]
    public async Task<ActionResult<CountryDto[]>> ListCountries(CancellationToken ct)
    {
        var countries = await db.Countries
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CountryDto(c.Iso2, c.Iso3, c.Name, c.Region))
            .ToArrayAsync(ct);
        return Ok(countries);
    }

    // ── Filter rail (for category pages) ───────────────────────────

    [HttpGet("filters")]
    public async Task<ActionResult<CategoryFiltersDto>> GetFilters(CancellationToken ct)
    {
        var certs = await db.CertificationBodies.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new CertificationBodyDto(
                b.Id, b.Name, b.Slug, b.Acronym, b.Country, (int)b.TrustTier, b.LogoUrl, b.Description))
            .ToArrayAsync(ct);

        var countries = await db.Countries.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CountryDto(c.Iso2, c.Iso3, c.Name, c.Region))
            .ToArrayAsync(ct);

        // The full set of dietary tags and statuses the platform supports
        var dietary = Enum.GetNames<DietaryTags>().Where(n => n != "None").ToArray();
        var statuses = Enum.GetNames<HalalStatus>().Where(n => n != "Unknown").ToArray();

        return Ok(new CategoryFiltersDto(certs, countries, dietary, statuses));
    }
}
