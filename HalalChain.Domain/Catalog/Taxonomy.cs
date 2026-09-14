namespace HalalChain.Domain.Catalog;

/// <summary>
/// Top-level commercial department. 8 of these in the marketplace —
/// forms the top navigation. Examples: "Food &amp; Beverage", "Modest Fashion &amp; Lifestyle".
/// </summary>
public sealed class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public string? HeroImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<TaxonomyCategory> Categories { get; set; } = [];
}

/// <summary>
/// Mid-tier commercial category. Examples: "Meat &amp; Poultry", "Dairy &amp; Eggs", "Hijabs".
/// Named <c>TaxonomyCategory</c> to disambiguate from the legacy flat <see cref="Category"/>.
/// </summary>
public sealed class TaxonomyCategory
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Subcategory> Subcategories { get; set; } = [];
}

/// <summary>
/// Lowest meaningful browse tier — the "filter chip" level.
/// Examples: "Beef", "Chicken", "Frozen", "Organic", "Loose-leaf".
/// </summary>
public sealed class Subcategory
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public TaxonomyCategory Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ProductType> ProductTypes { get; set; } = [];
}
