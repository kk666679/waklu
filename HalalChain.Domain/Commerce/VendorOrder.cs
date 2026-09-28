namespace HalalChain.Domain.Commerce;

using HalalChain.Domain.Common;

/// <summary>
/// The vendor's view of an order. One per vendor per order. Fulfilled
/// independently; triggers its own payout on completion.
/// </summary>
public sealed class VendorOrder : Entity<VendorOrderId>
{
    private readonly List<OrderItem> _items = [];

    public OrderId OrderId { get; private init; }
    public Guid VendorId { get; private init; }
    public VendorOrderStatus Status { get; private set; } = VendorOrderStatus.Pending;
    public decimal Subtotal { get; private init; }

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private VendorOrder() { }

    public static VendorOrder Create(
        OrderId orderId,
        Guid vendorId,
        IReadOnlyList<OrderItem> items,
        DateTimeOffset now) => new()
    {
        Id = VendorOrderId.New(),
        OrderId = orderId,
        VendorId = vendorId,
        Subtotal = items.Sum(i => i.LineTotal),
        Status = VendorOrderStatus.Pending,
        _items = [.. items],
    };

    public void MarkShipped(DateTimeOffset now)
    {
        if (Status != VendorOrderStatus.Pending)
            throw new InvalidOperationException($"Cannot ship vendor order in status {Status}.");
        Status = VendorOrderStatus.Shipped;
    }

    public void MarkDelivered(DateTimeOffset now)
    {
        if (Status != VendorOrderStatus.Shipped)
            throw new InvalidOperationException($"Cannot deliver vendor order in status {Status}.");
        Status = VendorOrderStatus.Delivered;
    }
}

public enum VendorOrderStatus
{
    Pending,
    Shipped,
    Delivered,
    Cancelled,
}
