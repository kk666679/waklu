namespace HalalChain.Domain.Commerce;

using HalalChain.Domain.Catalog;
using HalalChain.Domain.Common;

public sealed class CartItem : Entity<CartItemId>
{
    public Guid BuyerId { get; private init; }
    public ProductId ProductId { get; private init; }
    public ProductVariantId VariantId { get; private init; }
    public Guid VendorId { get; private init; }
    public int Quantity { get; private set; }

    private CartItem() { }

    public static CartItem Create(
        Guid buyerId,
        ProductId productId,
        ProductVariantId variantId,
        Guid vendorId,
        int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        return new CartItem
        {
            Id = CartItemId.New(),
            BuyerId = buyerId,
            ProductId = productId,
            VariantId = variantId,
            VendorId = vendorId,
            Quantity = quantity,
        };
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
    }
}
