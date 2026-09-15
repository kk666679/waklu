using HalalChain.Domain.Halal;
using HalalChain.Domain.Vendors;

namespace HalalChain.Domain.Catalog;

public sealed class Product
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    // ── Commerce path (new 4-level taxonomy) ────────────────────────
    public Guid? ProductTypeId { get; set; }
    public ProductType? ProductType { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    // ── Vendor / Brand / Facility ──────────────────────────────────
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }

    // ── Origin & pricing ────────────────────────────────────────────
    public string Origin { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MYR";
    public int Inventory { get; set; }

    public HalalProfile? HalalProfile { get; set; }

    // ── Variants ────────────────────────────────────────────────────
    public List<ProductVariant> Variants { get; set; } = [];

    // ── Relationships ──────────────────────────────────────────────
    public List<Certificate> Certificates { get; set; } = [];
    public List<HalalVerification> Verifications { get; set; } = [];
    public List<Commerce.CartItem> CartItems { get; set; } = [];
    public List<Commerce.OrderItem> OrderItems { get; set; } = [];
    public List<Commerce.WishlistItem> WishlistItems { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
