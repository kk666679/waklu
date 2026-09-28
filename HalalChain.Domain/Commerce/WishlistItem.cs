namespace HalalChain.Domain.Commerce;

using HalalChain.Domain.Catalog;
using HalalChain.Domain.Common;

public sealed class WishlistItem : Entity<CartItemId>
{
    public Guid BuyerId { get; private init; }
    public ProductId ProductId { get; private init; }
    public DateTimeOffset AddedAt { get; private init; }

    private WishlistItem() { }

    public static WishlistItem Create(Guid buyerId, ProductId productId, DateTimeOffset now) => new()
    {
        Id = CartItemId.New(),
        BuyerId = buyerId,
        ProductId = productId,
        AddedAt = now,
    };
}
