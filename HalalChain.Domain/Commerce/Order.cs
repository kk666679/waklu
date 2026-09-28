namespace HalalChain.Domain.Commerce;

public sealed class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped, Delivered, Cancelled
    public decimal Total { get; set; }
    public string Currency { get; set; } = "MYR";
    public string? ShippingAddress { get; set; }
    public string? PaymentToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<VendorOrder> VendorOrders { get; set; } = [];
}
