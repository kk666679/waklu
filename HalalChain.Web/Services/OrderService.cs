using HalalChain.Models;
using HalalChain.Realtime;

namespace HalalChain.Services;

public class OrderService : IOrderService
{
    private readonly List<Order> _orders = new();
    private readonly IRealtimeBroadcaster _realtime;
    private int _nextId = 1;

    public OrderService(IRealtimeBroadcaster realtime)
    {
        _realtime = realtime;
        for (var i = 1; i <= 6; i++)
        {
            _orders.Add(new Order
            {
                Id = _nextId++,
                OrderNumber = $"HC-{1000 + i}",
                UserId = $"user-{i}",
                Status = (OrderStatus)(i % 5),
                SubTotal = 50m + i * 5,
                Tax = 5m,
                ShippingFee = 3m,
                Total = 58m + i * 5
            });
        }
    }

    public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Order>>(_orders.ToList());

    public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));

    public Task<Order> CreateAsync(Order order, CancellationToken ct = default)
    {
        order.Id = _nextId++;
        order.OrderNumber = $"HC-{2000 + order.Id}";
        _orders.Add(order);
        return Task.FromResult(order);
    }

    public async Task<Order> UpdateStatusAsync(int id, OrderStatus status, CancellationToken ct = default)
    {
        var existing = _orders.FirstOrDefault(o => o.Id == id);
        if (existing is null) return new Order { Id = id };

        var previous = existing.Status;
        existing.Status = status;

        if (previous != status && !string.IsNullOrEmpty(existing.UserId))
        {
            try
            {
                var kind = status switch
                {
                    OrderStatus.Shipped => RealtimeNotificationKind.Success,
                    OrderStatus.Delivered => RealtimeNotificationKind.Success,
                    OrderStatus.Cancelled => RealtimeNotificationKind.Warning,
                    _ => RealtimeNotificationKind.Info
                };
                await _realtime.SendNotificationToUserAsync(
                    existing.UserId,
                    new RealtimeNotification(
                        "Order updated",
                        $"Order {existing.OrderNumber} is now {status}.",
                        kind,
                        Topic: $"orders.{existing.Id}"),
                    ct);
            }
            catch
            {
            }
        }

        return existing;
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _orders.RemoveAll(o => o.Id == id);
        return Task.CompletedTask;
    }
}
