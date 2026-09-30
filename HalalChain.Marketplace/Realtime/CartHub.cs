using HalalChain.Marketplace.Models.ViewModels;
using Microsoft.AspNetCore.SignalR;

namespace HalalChain.Marketplace.Realtime;

/// <summary>
/// SignalR hub for real-time cart updates.
/// Sends notifications when:
/// - Items are added/removed from cart
/// - Cart is updated or cleared
/// - Cart total changes
/// - Stock levels change affecting cart items
/// </summary>
public class CartHub : Hub
{
    private readonly ILogger<CartHub> _logger;

    public CartHub(ILogger<CartHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to cart hub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from cart hub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Join a customer's personal cart group for targeted updates.</summary>
    public async Task JoinCartGroup(Guid customerId)
    {
        var groupName = GetCartGroupName(customerId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined cart group {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>Leave a customer's cart group.</summary>
    public async Task LeaveCartGroup(Guid customerId)
    {
        var groupName = GetCartGroupName(customerId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left cart group {Group}", Context.ConnectionId, groupName);
    }

    /// <summary>Notify a customer that an item was added to their cart.</summary>
    public async Task NotifyItemAdded(Guid customerId, CartItemViewModel item)
    {
        var groupName = GetCartGroupName(customerId);
        await Clients.Group(groupName).SendAsync("ItemAdded", item);
    }

    /// <summary>Notify a customer that an item was removed from their cart.</summary>
    public async Task NotifyItemRemoved(Guid customerId, Guid itemId)
    {
        var groupName = GetCartGroupName(customerId);
        await Clients.Group(groupName).SendAsync("ItemRemoved", itemId);
    }

    /// <summary>Notify a customer that their cart was updated (quantity/price changes).</summary>
    public async Task NotifyCartUpdated(Guid customerId, CartSummaryViewModel cartSummary)
    {
        var groupName = GetCartGroupName(customerId);
        await Clients.Group(groupName).SendAsync("CartUpdated", cartSummary);
    }

    /// <summary>Notify a customer that their cart was cleared.</summary>
    public async Task NotifyCartCleared(Guid customerId)
    {
        var groupName = GetCartGroupName(customerId);
        await Clients.Group(groupName).SendAsync("CartCleared");
    }

    /// <summary>Notify customers that a product's stock level changed (may affect their cart).</summary>
    public async Task NotifyStockChanged(Guid productId, int newStockLevel)
    {
        await Clients.All.SendAsync("StockChanged", productId, newStockLevel);
    }

    /// <summary>Notify customers that a product went out of stock.</summary>
    public async Task NotifyOutOfStock(Guid productId, string productName)
    {
        await Clients.All.SendAsync("ProductOutOfStock", productId, productName);
    }

    private static string GetCartGroupName(Guid customerId) => $"cart-{customerId}";
}

/// <summary>Interface for triggering cart notifications from services.</summary>
public interface ICartNotificationService
{
    Task NotifyItemAddedAsync(Guid customerId, CartItemViewModel item);
    Task NotifyItemRemovedAsync(Guid customerId, Guid itemId);
    Task NotifyCartUpdatedAsync(Guid customerId, CartSummaryViewModel cartSummary);
    Task NotifyCartClearedAsync(Guid customerId);
    Task NotifyStockChangedAsync(Guid productId, int newStockLevel);
    Task NotifyOutOfStockAsync(Guid productId, string productName);
}

/// <summary>Service to send cart notifications via SignalR.</summary>
public class CartNotificationService : ICartNotificationService
{
    private readonly IHubContext<CartHub> _hubContext;
    private readonly ILogger<CartNotificationService> _logger;

    public CartNotificationService(IHubContext<CartHub> hubContext, ILogger<CartNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyItemAddedAsync(Guid customerId, CartItemViewModel item)
    {
        try
        {
            var groupName = $"cart-{customerId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ItemAdded", item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying item added");
        }
    }

    public async Task NotifyItemRemovedAsync(Guid customerId, Guid itemId)
    {
        try
        {
            var groupName = $"cart-{customerId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ItemRemoved", itemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying item removed");
        }
    }

    public async Task NotifyCartUpdatedAsync(Guid customerId, CartSummaryViewModel cartSummary)
    {
        try
        {
            var groupName = $"cart-{customerId}";
            await _hubContext.Clients.Group(groupName).SendAsync("CartUpdated", cartSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying cart updated");
        }
    }

    public async Task NotifyCartClearedAsync(Guid customerId)
    {
        try
        {
            var groupName = $"cart-{customerId}";
            await _hubContext.Clients.Group(groupName).SendAsync("CartCleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying cart cleared");
        }
    }

    public async Task NotifyStockChangedAsync(Guid productId, int newStockLevel)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("StockChanged", productId, newStockLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying stock changed");
        }
    }

    public async Task NotifyOutOfStockAsync(Guid productId, string productName)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ProductOutOfStock", productId, productName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying out of stock");
        }
    }
}
