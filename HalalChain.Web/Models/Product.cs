namespace HalalChain.Models;

/// <summary>
/// Enhanced product model (2026) with support for variants, sustainability,
/// rich media, dynamic pricing, and AI-driven features.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public Guid ExternalId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string MasterSku { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Keywords { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public string Currency { get; set; } = "MYR";
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public int VendorId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHalal { get; set; }
    public bool IsOutOfStock { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 2026 Features
    /// </summary>

    /// <summary>
    /// Average customer rating (0-5 stars).
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Total number of customer reviews.
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Product variants (different sizes, colors, weights, etc.).
    /// </summary>
    public List<ProductVariantModel> Variants { get; set; } = [];

    /// <summary>
    /// Sustainability and eco-rating data.
    /// </summary>
    public ProductSustainabilityModel? Sustainability { get; set; }

    /// <summary>
    /// Multiple media assets (images, videos, 360 views).
    /// </summary>
    public List<ProductMediaModel> MediaAssets { get; set; } = [];

    /// <summary>
    /// Product attributes for faceted search (color, size, material, etc.).
    /// </summary>
    public List<string> Attributes { get; set; } = [];

    /// <summary>
    /// Tags for organization and discovery (New, Bestseller, Trending, etc.).
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Ingredient list with halal status per ingredient.
    /// </summary>
    public List<string> Ingredients { get; set; } = [];

    /// <summary>
    /// Allergen warnings (comma-separated).
    /// </summary>
    public string? AllergenWarnings { get; set; }

    /// <summary>
    /// Dynamic pricing rules for promotions and A/B testing.
    /// </summary>
    public List<PricingRuleModel> PricingRules { get; set; } = [];

    /// <summary>
    /// AI-generated recommendation reason (shown in carousels).
    /// </summary>
    public string? RecommendationReason { get; set; }

    /// <summary>
    /// Whether this product has been indexed for semantic search.
    /// </summary>
    public bool IsSearchIndexed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Category? Category { get; set; }
    public Vendor? Vendor { get; set; }
    public HalalCertification? Certification { get; set; }
}

/// <summary>
/// Product variant model (size, color, weight, etc.).
/// </summary>
public class ProductVariantModel
{
    public int Id { get; set; }
    public Guid ExternalId { get; set; } = Guid.NewGuid();
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty; // "100g", "Red", "Medium"
    public string? VariantAttributes { get; set; } // JSON: {"weight":"100g","color":"red"}
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int Stock { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public decimal? NetQuantity { get; set; }
    public string? NetUnit { get; set; } // "g", "kg", "ml", "L"
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Sustainability and eco-rating data for a product.
/// </summary>
public class ProductSustainabilityModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? EcoRating { get; set; } // 1-5 stars
    public decimal? CarbonFootprintKgCo2 { get; set; }
    public List<string> Certifications { get; set; } = []; // Fair Trade, Organic, etc.
    public string? PackagingMaterial { get; set; }
    public int? RecyclablePercentage { get; set; }
    public bool IsFairTrade { get; set; }
    public bool IsVegan { get; set; }
    public string? OriginCountry { get; set; }
    public string? SustainabilityReportUrl { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}

/// <summary>
/// Media asset for a product (image, video, 360 view, etc.).
/// </summary>
public class ProductMediaModel
{
    public int Id { get; set; }
    public Guid ExternalId { get; set; } = Guid.NewGuid();
    public int ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = "Image"; // Image, Video, 360View, Demo
    public string? AltText { get; set; }
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public string? BlurHash { get; set; } // For blur-up loading effect
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Dynamic pricing rule for promotions and tiered discounts.
/// </summary>
public class PricingRuleModel
{
    public int Id { get; set; }
    public Guid ExternalId { get; set; } = Guid.NewGuid();
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RuleType { get; set; } = "Promotion"; // Promotion, TieredDiscount, ABTest
    public decimal PriceAdjustment { get; set; }
    public string AdjustmentType { get; set; } = "Percentage"; // Percentage or Fixed
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Condition { get; set; } // JSON for conditions
}
