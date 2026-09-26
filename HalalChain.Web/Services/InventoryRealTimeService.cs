namespace HalalChain.Services;

using HalalChain.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

/// <summary>
/// Real-time inventory management service.
/// Handles stock updates, reservations, low-stock alerts, and backorder management.
/// Uses SignalR for real-time WebSocket notifications to clients.
/// </summary>
public interface IInventoryRealTimeService
{
    /// <summary>
    /// Reserve stock for a cart item (hold for 15 minutes).
    /// </summary>
    Task<StockReservation> ReserveStockAsync(int productId, int quantity, string cartSessionId, CancellationToken ct = default);

    /// <summary>
    /// Release a stock reservation (if cart is abandoned).
    /// </summary>
    Task ReleaseReservationAsync(Guid reservationId, CancellationToken ct = default);

    /// <summary>
    /// Confirm stock deduction (convert reservation to actual sale).
    /// </summary>
    Task<bool> ConfirmStockAsync(Guid reservationId, int productId, int quantity, CancellationToken ct = default);

    /// <summary>
    /// Get current stock level for a product variant.
    /// </summary>
    Task<int> GetAvailableStockAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Check if product is low on stock.
    /// </summary>
    Task<bool> IsLowStockAsync(int productId, int threshold = 5, CancellationToken ct = default);

    /// <summary>
    /// Create a backorder for out-of-stock items.
    /// </summary>
    Task<Backorder> CreateBackorderAsync(int productId, string customerEmail, int quantity, CancellationToken ct = default);

    /// <summary>
    /// Notify all clients about stock level changes via SignalR.
    /// </summary>
    Task NotifyStockChangeAsync(int productId, int newStock, CancellationToken ct = default);

    /// <summary>
    /// Forecast inventory levels for next 7 days based on sales velocity.
    /// </summary>
    Task<InventoryForecast> ForecastInventoryAsync(int productId, CancellationToken ct = default);
}

/// <summary>
/// SignalR Hub for broadcasting real-time inventory updates to connected clients.
/// </summary>
public class InventoryHub : Hub
{
    public async Task SubscribeToProductStock(int productId)
    {
        // Subscribe user to product-specific inventory updates
        await Groups.AddToGroupAsync(Context.ConnectionId, $"product-{productId}");
    }

    public async Task UnsubscribeFromProductStock(int productId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"product-{productId}");
    }
}

/// <summary>
/// Implementation of real-time inventory service.
/// </summary>
public class InventoryRealTimeService : IInventoryRealTimeService
{
    private readonly IHubContext<InventoryHub> _hubContext;
    private readonly IProductService _productService;
    private readonly ILogger<InventoryRealTimeService> _logger;

    // In-memory stores (in production, use Redis or database)
    private readonly ConcurrentDictionary<Guid, StockReservation> _reservations = [];
    private readonly ConcurrentDictionary<int, List<Backorder>> _backorders = [];
    private readonly ConcurrentDictionary<int, InventoryAuditLog> _auditLogs = [];

    public InventoryRealTimeService(
        IHubContext<InventoryHub> hubContext,
        IProductService productService,
        ILogger<InventoryRealTimeService> logger)
    {
        _hubContext = hubContext;
        _productService = productService;
        _logger = logger;
    }

    public async Task<StockReservation> ReserveStockAsync(int productId, int quantity, string cartSessionId, CancellationToken ct = default)
    {
        var product = await _productService.GetByIdAsync(productId, ct);
        if (product == null)
            throw new InvalidOperationException($"Product {productId} not found.");

        if (product.StockQuantity < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {product.StockQuantity}, Requested: {quantity}");

        var reservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Quantity = quantity,
            CartSessionId = cartSessionId,
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15), // 15-minute hold
            Status = "Active"
        };

        _reservations.TryAdd(reservation.Id, reservation);

        // Deduct from available stock
        product.StockQuantity -= quantity;
        await _productService.UpdateAsync(product, ct);

        // Log the change
        LogInventoryChange(productId, -quantity, "Reservation", reservation.Id.ToString());

        // Notify clients of stock change
        await NotifyStockChangeAsync(productId, product.StockQuantity, ct);

        _logger.LogInformation("Stock reserved: ProductId={ProductId}, Quantity={Quantity}, ReservationId={ReservationId}",
            productId, quantity, reservation.Id);

        return reservation;
    }

    public async Task ReleaseReservationAsync(Guid reservationId, CancellationToken ct = default)
    {
        if (!_reservations.TryRemove(reservationId, out var reservation))
            return; // Already released or not found

        var product = await _productService.GetByIdAsync(reservation.ProductId, ct);
        if (product != null)
        {
            // Restore stock
            product.StockQuantity += reservation.Quantity;
            await _productService.UpdateAsync(product, ct);

            LogInventoryChange(reservation.ProductId, reservation.Quantity, "Reservation Released", reservationId.ToString());
            await NotifyStockChangeAsync(reservation.ProductId, product.StockQuantity, ct);

            _logger.LogInformation("Stock reservation released: ReservationId={ReservationId}, Quantity={Quantity}",
                reservationId, reservation.Quantity);
        }
    }

    public async Task<bool> ConfirmStockAsync(Guid reservationId, int productId, int quantity, CancellationToken ct = default)
    {
        if (!_reservations.TryRemove(reservationId, out var reservation))
            return false; // Reservation not found or expired

        if (reservation.Status != "Active")
            return false; // Already confirmed or cancelled

        reservation.Status = "Confirmed";
        LogInventoryChange(productId, 0, "Stock Confirmed (Sale)", reservationId.ToString());

        _logger.LogInformation("Stock confirmed for sale: ProductId={ProductId}, ReservationId={ReservationId}",
            productId, reservationId);

        // Notify clients
        var product = await _productService.GetByIdAsync(productId, ct);
        if (product != null)
        {
            await NotifyStockChangeAsync(productId, product.StockQuantity, ct);
        }

        return true;
    }

    public async Task<int> GetAvailableStockAsync(int productId, CancellationToken ct = default)
    {
        var product = await _productService.GetByIdAsync(productId, ct);
        if (product == null)
            return 0;

        // Subtract active reservations
        var reservedQty = _reservations.Values
            .Where(r => r.ProductId == productId && r.Status == "Active" && r.ExpiresAt > DateTime.UtcNow)
            .Sum(r => r.Quantity);

        return Math.Max(0, product.StockQuantity - reservedQty);
    }

    public async Task<bool> IsLowStockAsync(int productId, int threshold = 5, CancellationToken ct = default)
    {
        var availableStock = await GetAvailableStockAsync(productId, ct);
        return availableStock <= threshold;
    }

    public async Task<Backorder> CreateBackorderAsync(int productId, string customerEmail, int quantity, CancellationToken ct = default)
    {
        var backorder = new Backorder
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CustomerEmail = customerEmail,
            Quantity = quantity,
            OrderedAt = DateTime.UtcNow,
            Status = "Pending",
            NotificationsSent = 0
        };

        if (!_backorders.ContainsKey(productId))
        {
            _backorders[productId] = [];
        }

        _backorders[productId].Add(backorder);

        LogInventoryChange(productId, 0, "Backorder Created", backorder.Id.ToString());

        _logger.LogInformation("Backorder created: ProductId={ProductId}, BackorderId={BackorderId}, Email={Email}",
            productId, backorder.Id, customerEmail);

        return backorder;
    }

    public async Task NotifyStockChangeAsync(int productId, int newStock, CancellationToken ct = default)
    {
        var notification = new StockChangeNotification
        {
            ProductId = productId,
            NewStockLevel = newStock,
            Timestamp = DateTime.UtcNow,
            IsLowStock = newStock <= 5
        };

        try
        {
            // Broadcast to all clients subscribed to this product
            await _hubContext.Clients
                .Group($"product-{productId}")
                .SendAsync("StockUpdated", notification, cancellationToken: ct);

            _logger.LogDebug("Stock update broadcasted: ProductId={ProductId}, NewStock={NewStock}", productId, newStock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast stock update for ProductId={ProductId}", productId);
        }
    }

    public async Task<InventoryForecast> ForecastInventoryAsync(int productId, CancellationToken ct = default)
    {
        var product = await _productService.GetByIdAsync(productId, ct);
        if (product == null)
            throw new InvalidOperationException($"Product {productId} not found.");

        // Get audit logs for sales velocity
        if (!_auditLogs.TryGetValue(productId, out var auditLog))
        {
            auditLog = new InventoryAuditLog { ProductId = productId };
        }

        // Simple forecast: if average daily sales = 10 units, predict stockout in 5 days (50 units remaining / 10 = 5)
        var avgDailySales = 10; // Placeholder; in production, calculate from audit logs
        var daysUntilStockout = (int)(product.StockQuantity / (decimal)Math.Max(1, avgDailySales));

        var forecast = new InventoryForecast
        {
            ProductId = productId,
            CurrentStock = product.StockQuantity,
            AverageDailySales = avgDailySales,
            DaysUntilStockout = daysUntilStockout,
            StockoutPredicted = daysUntilStockout <= 7,
            GeneratedAt = DateTime.UtcNow
        };

        return forecast;
    }

    // Private helpers

    private void LogInventoryChange(int productId, int quantityChange, string changeType, string referenceId)
    {
        if (!_auditLogs.ContainsKey(productId))
        {
            _auditLogs[productId] = new InventoryAuditLog { ProductId = productId };
        }

        var log = _auditLogs[productId];
        log.Changes.Add(new InventoryChange
        {
            ChangeType = changeType,
            QuantityChange = quantityChange,
            Timestamp = DateTime.UtcNow,
            ReferenceId = referenceId
        });

        // Keep only last 1000 changes
        if (log.Changes.Count > 1000)
        {
            log.Changes = log.Changes.TakeLast(1000).ToList();
        }
    }
}

// Models

public class StockReservation
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string CartSessionId { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "Active"; // Active, Confirmed, Expired, Cancelled
}

public class Backorder
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime OrderedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Notified, Fulfilled, Cancelled
    public int NotificationsSent { get; set; }
}

public class StockChangeNotification
{
    public int ProductId { get; set; }
    public int NewStockLevel { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsLowStock { get; set; }
}

public class InventoryForecast
{
    public int ProductId { get; set; }
    public int CurrentStock { get; set; }
    public int AverageDailySales { get; set; }
    public int DaysUntilStockout { get; set; }
    public bool StockoutPredicted { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class InventoryAuditLog
{
    public int ProductId { get; set; }
    public List<InventoryChange> Changes { get; set; } = [];
}

public class InventoryChange
{
    public string ChangeType { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public DateTime Timestamp { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
}
