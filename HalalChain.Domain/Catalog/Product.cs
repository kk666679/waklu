using HalalChain.Domain.Halal;
using HalalChain.Domain.Vendors;

namespace HalalChain.Domain.Catalog;

public sealed class Product
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? Keywords { get; set; }

    // ── Commerce path (new 4-level taxonomy) ────────────────────────
    public Guid? ProductTypeId { get; set; }
    public ProductType? ProductType { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public List<Guid> SecondaryCategories { get; set; } = [];

    // ── Vendor / Brand / Facility ──────────────────────────────────
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }

    // ── Origin & pricing ────────────────────────────────────────────
    public string Origin { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string Currency { get; set; } = "MYR";
    public int Inventory { get; set; }

    // ── 2026 Enhancements: Ratings & Reviews ──────────────────────
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public HalalProfile? HalalProfile { get; set; }

    // ── Variants ────────────────────────────────────────────────────
    public List<ProductVariant> Variants { get; set; } = [];

    // ── 2026 Enhancements: Media Assets ────────────────────────────
    public List<ProductMediaAsset> MediaAssets { get; set; } = [];

    // ── 2026 Enhancements: Attributes & Faceted Search ──────────────
    public List<ProductAttributeValue> Attributes { get; set; } = [];

    // ── 2026 Enhancements: Sustainability & ESG ─────────────────────
    public ProductSustainability? Sustainability { get; set; }

    // ── 2026 Enhancements: Dynamic Pricing Rules ────────────────────
    public List<DynamicPricingRule> PricingRules { get; set; } = [];

    // ── 2026 Enhancements: Tags & Organization ──────────────────────
    public List<string> Tags { get; set; } = [];
    public List<string> Ingredients { get; set; } = [];
    public string? AllergenWarnings { get; set; }

    // ── 2026 Enhancements: Search & Indexing ──────────────────────
    public bool IsSearchIndexed { get; set; }
    public bool IsOutOfStock { get; set; }
    public string? RecommendationReason { get; set; }

    // ── Relationships ──────────────────────────────────────────────
    public List<Certificate> Certificates { get; set; } = [];
    public List<HalalVerification> Verifications { get; set; } = [];
    public List<Commerce.CartItem> CartItems { get; set; } = [];
    public List<Commerce.OrderItem> OrderItems { get; set; } = [];
    public List<Commerce.WishlistItem> WishlistItems { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
