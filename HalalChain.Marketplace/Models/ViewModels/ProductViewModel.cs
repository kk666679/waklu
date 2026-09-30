namespace HalalChain.Marketplace.Models.ViewModels;

/// <summary>ViewModel for displaying a Product with all related data safe for Blazor binding.</summary>
public class ProductViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string Currency { get; set; } = "MYR";
    public int Inventory { get; set; }
    public bool IsOutOfStock { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string? Keywords { get; set; }

    // Relationships
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid? ProductTypeId { get; set; }
    public string? ProductTypeName { get; set; }

    // Halal Profile
    public string? HalalStatus { get; set; }
    public string? DietaryTags { get; set; }
    public string? CertificateNumber { get; set; }
    public DateTime? CertificateExpiry { get; set; }

    // Allergens & Ingredients
    public string? AllergenWarnings { get; set; }

    // Media
    public List<ProductMediaViewModel> MediaAssets { get; set; } = [];

    // Variants
    public List<ProductVariantViewModel> Variants { get; set; } = [];

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>ViewModel for Product variant (size, weight, color, etc.).</summary>
public class ProductVariantViewModel
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? VariantAttributes { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string Currency { get; set; } = "MYR";
    public int Stock { get; set; }
    public int LowStockThreshold { get; set; }
    public string? NetQuantity { get; set; }
    public string? NetUnit { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>ViewModel for Product media assets (images, videos, etc.).</summary>
public class ProductMediaViewModel
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = "Image"; // Image, Video, 360View, Demo, Instruction, ThreeDModel
    public string? AltText { get; set; }
    public string? Caption { get; set; }
    public string? BlurHash { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public int? VideoDurationSeconds { get; set; }
    public string? VideoThumbnailUrl { get; set; }
}

/// <summary>ViewModel for displaying a single product in list/search results (lightweight).</summary>
public class ProductSummaryViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string Currency { get; set; } = "MYR";
    public bool IsOutOfStock { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public string? PrimaryImageBlurHash { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? HalalStatus { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>ViewModel for creating/editing a product (admin).</summary>
public class ProductFormViewModel
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string Currency { get; set; } = "MYR";
    public int Inventory { get; set; }
    public string? Keywords { get; set; }
    public string? Origin { get; set; }

    // Foreign keys
    public Guid VendorId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ProductTypeId { get; set; }
    public Guid? CategoryId { get; set; }

    // Halal
    public string? HalalStatus { get; set; }
    public string? CertificateNumber { get; set; }
    public string? CertificationBody { get; set; }
    public DateTime? CertificateExpiry { get; set; }
    public string? DietaryTags { get; set; }

    // Ingredients/Allergens
    public List<string> Ingredients { get; set; } = [];
    public string? AllergenWarnings { get; set; }

    // Tags
    public List<string> Tags { get; set; } = [];
}
