using HalalChain.Domain.Catalog;

namespace HalalChain.Domain.Commerce;

public sealed class CartItem
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
