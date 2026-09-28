using HalalChain.Domain.Catalog;
using HalalChain.Domain.Vendors;

namespace HalalChain.Domain.Commerce;

public sealed class VendorOrder
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public decimal Subtotal { get; set; }
    public string Currency { get; set; } = "MYR";
    public string? TrackingNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; set; } = [];
}
