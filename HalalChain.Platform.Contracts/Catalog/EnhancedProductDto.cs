namespace HalalChain.Platform.Contracts.Catalog;

/// <summary>
/// Enhanced product DTO for 2026 e-commerce catalog with full support for:
/// - Multi-variant management
/// - Sustainability tracking
/// - Rich media assets (images, videos, 360 views)
/// - Dynamic attributes and faceted search
/// - AI-powered descriptions and recommendations
/// </summary>
public sealed class EnhancedProductDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Master SKU for the product (parent identifier).
    /// Individual variants have their own SKUs.
    /// </summary>
    public string MasterSku { get; set; } = string.Empty;

    /// <summary>
    /// Short description (one-liner for listings).
    /// </summary>
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Long, rich description with HTML formatting.
    /// May be AI-generated or vendor-provided.
    /// </summary>
    public string LongDescription { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated keywords for SEO and search.
    /// Example: "organic dates, premium halal, certified"
    /// </summary>
    public string? Keywords { get; set; }

    /// <summary>
    /// Primary category ID for classification.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Optional secondary categories.
    /// </summary>
    public List<Guid> SecondaryCategories { get; set; } = [];

    /// <summary>
    /// Vendor/seller ID.
    /// </summary>
    public Guid VendorId { get; set; }

    /// <summary>
    /// Base price (typically lowest variant price).
    /// Displayed as "from $X.XX".
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Currency code (e.g., "MYR", "USD", "SGD").
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Compare-at price for showing MSRP strikethrough.
    /// </summary>
    public decimal? CompareAtPrice { get; set; }

    /// <summary>
    /// Average rating from customer reviews (0-5).
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Total number of reviews.
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Product variants (sizes, colors, weights, etc.).
    /// Each variant has independent SKU, price, and stock.
    /// </summary>
    public List<ProductVariant> Variants { get; set; } = [];

    /// <summary>
    /// Product attributes for faceted search and filtering.
    /// </summary>
    public List<ProductAttributeValueDto> Attributes { get; set; } = [];

    /// <summary>
    /// Media assets: images, videos, 360 views, instruction videos.
    /// </summary>
    public List<MediaAssetDto> MediaAssets { get; set; } = [];

    /// <summary>
    /// Halal certification and compliance details.
    /// </summary>
    public HalalCertificationDto? HalalCertification { get; set; }

    /// <summary>
    /// Sustainability and environmental metrics.
    /// </summary>
    public SustainabilityDto? Sustainability { get; set; }

    /// <summary>
    /// Dynamic pricing rules applicable to this product.
    /// Used for promotions, tiered discounts, A/B testing.
    /// </summary>
    public List<DynamicPricingRuleDto> PricingRules { get; set; } = [];

    /// <summary>
    /// Whether the product is currently available for sale.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether all variants are out of stock (convenience flag).
    /// </summary>
    public bool IsOutOfStock { get; set; }

    /// <summary>
    /// Tags for organization and discovery.
    /// Examples: ["New Arrival", "Bestseller", "On Sale", "Trending"]
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// AI-generated recommendation reason (shown in recommendation carousels).
    /// Example: "You browsed similar products", "Trending in your region".
    /// </summary>
    public string? RecommendationReason { get; set; }

    /// <summary>
    /// Allergen warnings (comma-separated list).
    /// Example: "Peanuts, Tree nuts, Sesame"
    /// </summary>
    public string? AllergenWarnings { get; set; }

    /// <summary>
    /// Ingredient list with halal status annotations per ingredient.
    /// </summary>
    public List<IngredientDto> Ingredients { get; set; } = [];

    /// <summary>
    /// Nutritional information per serving/100g.
    /// </summary>
    public NutritionInfoDto? NutritionInfo { get; set; }

    /// <summary>
    /// Frequently asked questions generated from reviews + AI.
    /// </summary>
    public List<ProductFaqDto> Faqs { get; set; } = [];

    /// <summary>
    /// Compliance flags for policies and regulations.
    /// </summary>
    public ComplianceDto? Compliance { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Version number for tracking schema changes.
    /// </summary>
    public int SchemaVersion { get; set; } = 2;
}

/// <summary>
/// Ingredient with halal verification status.
/// </summary>
public sealed class IngredientDto
{
    public string Name { get; set; } = string.Empty;
    public string? Percentage { get; set; }
    public HalalStatus HalalStatus { get; set; }
    public string? SourceCountry { get; set; }
    public bool IsAllergen { get; set; }
}

/// <summary>
/// Nutritional information per serving or 100g.
/// </summary>
public sealed class NutritionInfoDto
{
    public string? ServingSize { get; set; }
    public int? Calories { get; set; }
    public decimal? ProteinG { get; set; }
    public decimal? FatG { get; set; }
    public decimal? CarbsG { get; set; }
    public decimal? FiberG { get; set; }
    public decimal? SugarG { get; set; }
    public decimal? SodiumMg { get; set; }
}

/// <summary>
/// Frequently asked question about the product.
/// </summary>
public sealed class ProductFaqDto
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Source { get; set; } = "Vendor"; // "Vendor" or "AI-Generated"
    public int Votes { get; set; } // Upvotes from customers
}

/// <summary>
/// Compliance and regulatory information.
/// </summary>
public sealed class ComplianceDto
{
    public bool RequiresBatteryWarning { get; set; }
    public bool RequiresChokingHazardWarning { get; set; }
    public bool IsProprietary { get; set; } // Requires review/approval
    public List<string> ApplicableRegulations { get; set; } = []; // "GDPR", "FDA", "CE"
}

/// <summary>
/// Dynamic pricing rule for promotions, tiered discounts, and A/B testing.
/// </summary>
public sealed class DynamicPricingRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RuleType { get; set; } = "Promotion"; // "Promotion", "TieredDiscount", "ABTest"
    public decimal PriceAdjustment { get; set; }
    public string? AdjustmentType { get; set; } = "Percentage"; // "Percentage" or "Fixed"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Condition { get; set; } // JSON: {"minQuantity": 5, "customerSegment": "wholesale"}
}

/// <summary>
/// Halal certification details.
/// </summary>
public sealed class HalalCertificationDto
{
    public HalalStatus Status { get; set; }
    public string CertifyingBody { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public DateTime IssuedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? CertificateUrl { get; set; }
}
