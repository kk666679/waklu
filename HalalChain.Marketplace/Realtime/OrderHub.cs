using HalalChain.Marketplace.Models.ViewModels;
using Microsoft.AspNetCore.SignalR;

namespace HalalChain.Marketplace.Realtime;

/// <summary>
/// SignalR hub for real-time order updates.
/// Sends notifications when:
/// - Order status changes (Pending -> Processing -> Shipped -> Delivered)
/// - Order is cancelled
/// - Tracking information is updated
/// - Vendor updates order fulfillment status
/// </summary>
public class OrderHub : Hub
{
    private readonly ILogger<OrderHub> _logger;

    public OrderHub(ILogger<OrderHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to order hub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from order hub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Join an order-specific group for targeted updates (customers tracking order, vendors managing it).</summary>
    public async Task JoinOrderGroup(Guid orderId)
    {
        var groupName = GetOrderGroupName(orderId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined order group {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>Leave an order group.</summary>
    public async Task LeaveOrderGroup(Guid orderId)
    {
        var groupName = GetOrderGroupName(orderId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left order group {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>Notify order members that order status changed.</summary>
    public async Task NotifyOrderStatusChanged(Guid orderId, string newStatus, string? reason = null)
    {
        var groupName = GetOrderGroupName(orderId);
        await Clients.Group(groupName).SendAsync("OrderStatusChanged", orderId, newStatus, reason);
    }

    /// <summary>Notify order members that a vendor order was updated.</summary>
    public async Task NotifyVendorOrderUpdated(Guid orderId, string vendorName, string status)
    {
        var groupName = GetOrderGroupName(orderId);
        await Clients.Group(groupName).SendAsync("VendorOrderUpdated", vendorName, status);
    }

    /// <summary>Notify customer that tracking information is available.</summary>
    public async Task NotifyTrackingUpdated(Guid orderId, string? trackingNumber, string? carrierName, string? trackingUrl)
    {
        var groupName = GetOrderGroupName(orderId);
        await Clients.Group(groupName).SendAsync("TrackingUpdated", trackingNumber, carrierName, trackingUrl);
    }

    /// <summary>Notify customer and vendor that order was delivered.</summary>
    public async Task NotifyOrderDelivered(Guid orderId, string customerName)
    {
        var groupName = GetOrderGroupName(orderId);
        await Clients.Group(groupName).SendAsync("OrderDelivered", customerName);
    }

    /// <summary>Notify vendor that new order was placed.</summary>
    public async Task NotifyOrderPlaced(Guid vendorId, OrderViewModel order)
    {
        var groupName = GetVendorOrdersGroupName(vendorId);
        await Clients.Group(groupName).SendAsync("OrderPlaced", order);
    }

    /// <summary>Join vendor's orders group to receive order notifications.</summary>
    public async Task JoinVendorOrdersGroup(Guid vendorId)
    {
        var groupName = GetVendorOrdersGroupName(vendorId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined vendor orders group {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>Leave vendor's orders group.</summary>
    public async Task LeaveVendorOrdersGroup(Guid vendorId)
    {
        var groupName = GetVendorOrdersGroupName(vendorId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    private static string GetOrderGroupName(Guid orderId) => $"order-{orderId}";
    private static string GetVendorOrdersGroupName(Guid vendorId) => $"vendor-orders-{vendorId}";
}

/// <summary>Interface for triggering order notifications from services.</summary>
public interface IOrderNotificationService
{
    Task NotifyOrderStatusChangedAsync(Guid orderId, string newStatus, string? reason = null);
    Task NotifyVendorOrderUpdatedAsync(Guid orderId, string vendorName, string status);
    Task NotifyTrackingUpdatedAsync(Guid orderId, string? trackingNumber, string? carrierName, string? trackingUrl);
    Task NotifyOrderDeliveredAsync(Guid orderId, string customerName);
    Task NotifyOrderPlacedAsync(Guid vendorId, OrderViewModel order);
}

/// <summary>Service to send order notifications via SignalR.</summary>
public class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<OrderNotificationService> _logger;

    public OrderNotificationService(IHubContext<OrderHub> hubContext, ILogger<OrderNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyOrderStatusChangedAsync(Guid orderId, string newStatus, string? reason = null)
    {
        try
        {
            var groupName = $"order-{orderId}";
            await _hubContext.Clients.Group(groupName).SendAsync("OrderStatusChanged", orderId, newStatus, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying order status changed");
        }
    }

    public async Task NotifyVendorOrderUpdatedAsync(Guid orderId, string vendorName, string status)
    {
        try
        {
            var groupName = $"order-{orderId}";
            await _hubContext.Clients.Group(groupName).SendAsync("VendorOrderUpdated", vendorName, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying vendor order updated");
        }
    }

    public async Task NotifyTrackingUpdatedAsync(Guid orderId, string? trackingNumber, string? carrierName, string? trackingUrl)
    {
        try
        {
            var groupName = $"order-{orderId}";
            await _hubContext.Clients.Group(groupName).SendAsync("TrackingUpdated", trackingNumber, carrierName, trackingUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying tracking updated");
        }
    }

    public async Task NotifyOrderDeliveredAsync(Guid orderId, string customerName)
    {
        try
        {
            var groupName = $"order-{orderId}";
            await _hubContext.Clients.Group(groupName).SendAsync("OrderDelivered", customerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying order delivered");
        }
    }

    public async Task NotifyOrderPlacedAsync(Guid vendorId, OrderViewModel order)
    {
        try
        {
            var groupName = $"vendor-orders-{vendorId}";
            await _hubContext.Clients.Group(groupName).SendAsync("OrderPlaced", order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying order placed");
        }
    }
}
