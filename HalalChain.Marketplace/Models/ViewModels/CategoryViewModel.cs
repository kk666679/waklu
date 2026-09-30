namespace HalalChain.Marketplace.Models.ViewModels;

/// <summary>ViewModel for Department (top-level commerce category).</summary>
public class DepartmentViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public string? HeroImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<TaxonomyCategoryViewModel> Categories { get; set; } = [];
}

/// <summary>ViewModel for TaxonomyCategory (mid-tier commerce category).</summary>
public class TaxonomyCategoryViewModel
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<SubcategoryViewModel> Subcategories { get; set; } = [];
}

/// <summary>ViewModel for Subcategory (filter-chip level, e.g., "Beef", "Organic").</summary>
public class SubcategoryViewModel
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<ProductTypeViewModel> ProductTypes { get; set; } = [];
}

/// <summary>ViewModel for ProductType (atomic product classification).</summary>
public class ProductTypeViewModel
{
    public Guid Id { get; set; }
    public Guid SubcategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PathSlug { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>ViewModel for BrandSummary (display in catalog).</summary>
public class BrandSummaryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? CountryOfOrigin { get; set; }
    public int ProductCount { get; set; }
}

/// <summary>ViewModel for BrandDetail (brand page).</summary>
public class BrandDetailViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? WebsiteUrl { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int ProductCount { get; set; }
    public List<ProductSummaryViewModel> Products { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

/// <summary>ViewModel for CategoryBrowse (category with product counts and filters).</summary>
public class CategoryBrowseViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Breadcrumb path
    public List<BreadcrumbItem> Breadcrumbs { get; set; } = [];
    
    // Filterable attributes
    public List<FilterAttributeViewModel> Attributes { get; set; } = [];
    
    // Subcategories as filter chips
    public List<SubcategoryViewModel> Subcategories { get; set; } = [];
    
    // Products
    public List<ProductSummaryViewModel> Products { get; set; } = [];
    public int TotalProductCount { get; set; }
    public int PageSize { get; set; } = 24;
}

public class BreadcrumbItem
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

/// <summary>ViewModel for faceted search attribute (filter option).</summary>
public class FilterAttributeViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Select, MultiSelect, Range, Text
    public List<FilterOptionViewModel> Options { get; set; } = [];
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public string? Unit { get; set; }
}

public class FilterOptionViewModel
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsSelected { get; set; }
}
