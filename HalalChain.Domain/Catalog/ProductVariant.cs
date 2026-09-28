namespace HalalChain.Domain.Catalog;

/// <summary>
/// A sellable variant of a product (e.g. 100g, 250g, 500g, 1kg, or
/// "Pack of 6", "Red / Blue / Green", "50ml / 100ml"). Each variant has
/// its own SKU, barcode, price, stock, and wholesale pricing tiers.
/// </summary>
public sealed class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? VariantAttributes { get; set; }

    public decimal Price { get; set; }
    public string Currency { get; set; } = "MYR";

    /// <summary>Compare-at price for showing a strikethrough MSRP.</summary>
    public decimal? CompareAtPrice { get; set; }

    public int Stock { get; set; }
    public int LowStockThreshold { get; set; } = 5;

    /// <summary>Net weight/volume for shipping calculations. Optional.</summary>
    public decimal? NetQuantity { get; set; }
    public string? NetUnit { get; set; }

    public List<WholesalePriceTier> WholesaleTiers { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
