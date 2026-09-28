using HalalChain.Domain.Catalog;

namespace HalalChain.Domain.Commerce;

public sealed class OrderItem
{
    public Guid Id { get; set; }
    public Guid VendorOrderId { get; set; }
    public VendorOrder VendorOrder { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "MYR";
}
