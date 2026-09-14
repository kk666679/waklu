using HalalChain.Platform.Contracts.Catalog.Dto;

namespace HalalChain.Services;

public class ReviewDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string CustomerName { get; set; } = "Anonymous";
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public bool IsVerifiedPurchase { get; set; }
}

public class ProductVariant
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "text"; // text, color, image
    public List<VariantOption> Options { get; set; } = [];
}

public class VariantOption
{
    public string Value { get; set; } = "";
    public string? Color { get; set; }
    public bool IsSelected { get; set; }
    public decimal? PriceAdjustment { get; set; }
    public int? InventoryAdjustment { get; set; }
}

public class ExtendedProductDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string CategoryName { get; set; } = "";
    public string CategorySlug { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string VendorSlug { get; set; } = "";
    public string Origin { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string Currency { get; set; } = "MYR";
    public int Inventory { get; set; }
    public decimal? DiscountPercentage => CompareAtPrice.HasValue && CompareAtPrice > Price
        ? Math.Round((1 - Price / CompareAtPrice.Value) * 100) : null;
    public HalalStatusDto? HalalStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsNew => CreatedAt > DateTimeOffset.UtcNow.AddDays(-30);
    public double? Rating { get; set; }
    public int ReviewCount { get; set; }
    public string? MainImageUrl { get; set; }
    public List<string> Images { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<ProductVariant> VariantAttributes { get; set; } = [];
    public List<ProductVariant>? Variants { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public string? BlockchainBadgeTokenId { get; set; }
    public bool HasBlockchainBadge => !string.IsNullOrEmpty(BlockchainBadgeTokenId);

    public static ExtendedProductDto FromProductDto(ProductDto p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Slug = p.Slug,
        Description = p.Description,
        CategoryName = p.CategoryName,
        CategorySlug = p.CategoryName.ToLower().Replace(" & ", "-").Replace(" ", "-"),
        VendorName = p.VendorName,
        VendorSlug = p.VendorName.ToLower().Replace(" ", "-"),
        Origin = p.Origin,
        Price = p.Price,
        Currency = p.Currency,
        Inventory = p.Inventory,
        HalalStatus = p.HalalStatus,
        CreatedAt = p.CreatedAt,
        MainImageUrl = $"/images/products/{p.Slug}.jpg",
        Images = [$"/images/products/{p.Slug}.jpg"],
        Tags = [p.CategoryName.ToLower(), p.VendorName.ToLower(), "halal"],
        Metadata = new()
        {
            ["Origin"] = p.Origin,
            ["Category"] = p.CategoryName,
            ["Vendor"] = p.VendorName,
            ["SKU"] = $"HC-{p.Slug.ToUpper()}"
        }
    };
}
