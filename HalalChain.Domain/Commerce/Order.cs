namespace HalalChain.Domain.Commerce;

using HalalChain.Domain.Catalog;
using HalalChain.Domain.Common;

/// <summary>
/// The buyer-facing order. Splits into VendorOrders, one per vendor,
/// because a multi-vendor cart produces a single payment capture but
/// per-vendor fulfilment and payouts.
/// </summary>
public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderItem> _items = [];
    private readonly List<VendorOrder> _vendorOrders = [];

    public Guid BuyerId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "MYR";
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public DateTimeOffset PlacedAt { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyList<VendorOrder> VendorOrders => _vendorOrders.AsReadOnly();

    private Order() { }

    public static Order Place(
        Guid buyerId,
        IReadOnlyList<OrderItem> items,
        string currency,
        DateTimeOffset now)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("Order must have at least one item.");

        var order = new Order
        {
            Id = OrderId.New(),
            BuyerId = buyerId,
            Currency = currency,
            TotalAmount = items.Sum(i => i.LineTotal),
            PlacedAt = now,
            Status = OrderStatus.Pending,
        };

        order._items.AddRange(items);
        order.SplitByVendor(now);
        order.Raise(new OrderPlaced(order.Id, buyerId, order.TotalAmount, now));
        return order;
    }

    /// <summary>
    /// Splits the order into one VendorOrder per distinct vendor. Called
    /// once, at placement. The split is deterministic — it depends only
    /// on the vendor IDs in the items.
    /// </summary>
    private void SplitByVendor(DateTimeOffset now)
    {
        var groups = _items.GroupBy(i => i.VendorId);
        foreach (var group in groups)
        {
            var vendorOrder = VendorOrder.Create(Id, group.Key, group.ToList(), now);
            _vendorOrders.Add(vendorOrder);
        }
    }

    public void Confirm(DateTimeOffset now)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm order in status {Status}.");
        Status = OrderStatus.Confirmed;
        Raise(new OrderConfirmed(Id, now));
    }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    PartiallyFulfilled,
    Fulfilled,
    Cancelled,
}
