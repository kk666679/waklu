namespace HalalChain.Domain.Commerce;

using HalalChain.Domain.Catalog;
using HalalChain.Domain.Common;

public sealed class OrderItem : Entity<OrderItemId>
{
    public ProductId ProductId { get; private init; }
    public ProductVariantId VariantId { get; private init; }
    public Guid VendorId { get; private init; }
    public string Sku { get; private init; } = string.Empty;
    public string ProductTitle { get; private init; } = string.Empty;
    public decimal UnitPrice { get; private init; }
    public int Quantity { get; private init; }

    public decimal LineTotal => UnitPrice * Quantity;

    private OrderItem() { }

    public static OrderItem Create(
        ProductId productId,
        ProductVariantId variantId,
        Guid vendorId,
        string sku,
        string productTitle,
        decimal unitPrice,
        int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        return new OrderItem
        {
            Id = OrderItemId.New(),
            ProductId = productId,
            VariantId = variantId,
            VendorId = vendorId,
            Sku = sku,
            ProductTitle = productTitle,
            UnitPrice = unitPrice,
            Quantity = quantity,
        };
    }
}
