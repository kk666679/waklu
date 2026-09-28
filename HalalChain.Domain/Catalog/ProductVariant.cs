namespace HalalChain.Domain.Catalog;

using HalalChain.Domain.Common;

public sealed class ProductVariant : Entity<ProductVariantId>
{
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "MYR";
    public int StockOnHand { get; private set; }

    private ProductVariant() { }

    public static ProductVariant Create(string sku, string name, decimal price, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));

        return new ProductVariant
        {
            Id = ProductVariantId.New(),
            Sku = sku,
            Name = name,
            Price = price,
            Currency = currency,
        };
    }

    public void AdjustStock(int delta)
    {
        if (StockOnHand + delta < 0)
            throw new InvalidOperationException("Stock cannot go negative.");
        StockOnHand += delta;
    }
}
