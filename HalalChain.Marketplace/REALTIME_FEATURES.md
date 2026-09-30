# Real-time Features via SignalR

This document describes the real-time communication features in HalalChain.Marketplace using ASP.NET Core SignalR.

## Overview

Real-time features enable instant updates across multiple user sessions:
- **Cart Hub** (`/hubs/cart`) - Cart synchronization across devices
- **Order Hub** (`/hubs/orders`) - Order status and tracking updates
- **Notification Hub** (`/hubs/notifications`) - General notifications
- **Automatic Reconnection** - Clients automatically reconnect if connection drops
- **Graceful Degradation** - App works without real-time (features optional)

## Architecture

### Hub Structure

Each hub manages specific real-time events:

```
┌─────────────────────────────────────────────┐
│        SignalR Hubs (Server-side)          │
├──────────────┬──────────────┬──────────────┤
│  CartHub     │   OrderHub   │ NotificationHub
│  /hubs/cart  │ /hubs/orders │ /hubs/notif...
└──────────────┴──────────────┴──────────────┘
        │              │              │
        └──────────────┼──────────────┘
                       │
            ┌──────────┴──────────┐
            │   Hub Groups        │
            ├─────────────────────┤
            │ cart-{customerId}   │
            │ order-{orderId}     │
            │ vendor-orders-{id}  │
            └─────────────────────┘
```

### Client Bridge Components

Blazor components establish connections and handle updates:

```
CartRealtimeBridge.razor
  ↓
Subscribe to "/hubs/cart"
  ↓
Join "cart-{customerId}" group
  ↓
Listen for: ItemAdded, ItemRemoved, CartUpdated, StockChanged, etc.
  ↓
Trigger component StateHasChanged() when events received
```

## Hubs

### 1. CartHub (`/hubs/cart`)

**Purpose**: Synchronize shopping cart across customer's devices

#### Server-side Methods (called by client):
```csharp
JoinCartGroup(Guid customerId)         // Join personal cart group
LeaveCartGroup(Guid customerId)        // Leave personal cart group
```

#### Server Events (sent to client):
```csharp
ItemAdded(CartItemViewModel item)              // Item added to cart
ItemRemoved(Guid itemId)                       // Item removed from cart
CartUpdated(CartSummaryViewModel cartSummary) // Cart totals updated
CartCleared()                                  // Cart was cleared
StockChanged(Guid productId, int newLevel)    // Product stock changed
ProductOutOfStock(Guid productId, string name) // Product went out of stock
```

#### Data Flow:
```
Customer adds item to cart
  ↓
CartRepository.AddAsync() stores in DB
  ↓
CartNotificationService.NotifyItemAddedAsync() sends to cart group
  ↓
All customer's devices receive ItemAdded event
  ↓
CartRealtimeBridge.razor calls StateHasChanged()
  ↓
UI updates to reflect new cart item
```

#### Implementation:
```csharp
// Server-side: ProductRepository detects stock change
await CartNotificationService.NotifyStockChangedAsync(productId, newStockLevel);

// Notification service sends to hub
await _hubContext.Clients.All.SendAsync("StockChanged", productId, newStockLevel);

// Client receives event
_hubConnection.On<Guid, int>("StockChanged", OnStockChanged);
```

### 2. OrderHub (`/hubs/orders`)

**Purpose**: Notify customers and vendors about order status changes

#### Server-side Methods (called by client):
```csharp
JoinOrderGroup(Guid orderId)           // Join order group
LeaveOrderGroup(Guid orderId)          // Leave order group
JoinVendorOrdersGroup(Guid vendorId)   // Join vendor's orders group
LeaveVendorOrdersGroup(Guid vendorId)  // Leave vendor's orders group
```

#### Server Events (sent to client):
```csharp
OrderStatusChanged(Guid orderId, string newStatus, string? reason)  // Status changed
VendorOrderUpdated(string vendorName, string status)               // Vendor updated
TrackingUpdated(string? number, string? carrier, string? url)      // Tracking info
OrderDelivered(string customerName)                                // Order delivered
OrderPlaced(OrderViewModel order)                                  // New order placed
```

#### Order Groups:
- **Order Group** (`order-{orderId}`): Contains customer and all vendor connections for an order
- **Vendor Orders Group** (`vendor-orders-{vendorId}`): Contains all vendor staff viewing order dashboard

#### Status Progression:
```
Pending → Processing → Shipped → Delivered
   ↓           ↓          ↓          ↓
 Event      Event      Event      Event
```

### 3. NotificationHub (`/hubs/notifications`)

**Purpose**: Broadcast general system notifications

#### Server Events (sent to client):
```csharp
Broadcast(string title, string body, string style)  // General notification
```

## Bridge Components

### CartRealtimeBridge.razor

**Purpose**: Automatically syncs cart across customer's sessions

**Location**: `Components/Shared/CartRealtimeBridge.razor`

**Usage in Layout**:
```razor
<CartRealtimeBridge />

@code {
    [Inject] private CartRealtimeBridge Bridge { get; set; } = default!;
}
```

**Features**:
- Connects to `/hubs/cart` on initialization
- Joins customer-specific cart group
- Handles item additions, removals, and stock changes
- Automatically reconnects on connection loss
- Gracefully degrades if connection unavailable

**Events Handled**:
- `ItemAdded` → Show toast, update cart count
- `ItemRemoved` → Update cart, show notification
- `CartUpdated` → Update cart totals and items
- `StockChanged` → Update product availability
- `ProductOutOfStock` → Show warning

### OrderRealtimeBridge.razor

**Purpose**: Track order status and shipping updates

**Location**: `Components/Shared/OrderRealtimeBridge.razor`

**Usage in Order Detail Page**:
```razor
<OrderRealtimeBridge OrderId="@OrderId" VendorId="@VendorId"
    OnOrderStatusChanged="@HandleStatusChange"
    OnTrackingUpdated="@HandleTrackingUpdate" />

@code {
    private void HandleStatusChange(OrderRealtimeBridge.OrderStatusUpdate update)
    {
        // Show "Order shipped!" notification
        Toast.Success($"Order status: {update.NewStatus}");
    }

    private void HandleTrackingUpdate(OrderRealtimeBridge.TrackingUpdate update)
    {
        // Show tracking link
        TrackingNumber = update.TrackingNumber;
    }
}
```

**Parameters**:
- `OrderId` (Guid, required) - Order to track
- `VendorId` (Guid?, optional) - If vendor viewing their orders
- `OnOrderStatusChanged` (EventCallback) - Status update handler
- `OnTrackingUpdated` (EventCallback) - Tracking update handler

**Events Handled**:
- `OrderStatusChanged` → Show status update notification
- `VendorOrderUpdated` → Show vendor fulfillment status
- `TrackingUpdated` → Update tracking display with link
- `OrderDelivered` → Show delivery confirmation

## Notification Services

### ICartNotificationService

Injected into repositories/services to send cart updates:

```csharp
@inject ICartNotificationService CartNotifier

// When item added to cart:
await CartNotifier.NotifyItemAddedAsync(customerId, cartItem);

// When product stock changes:
await CartNotifier.NotifyStockChangedAsync(productId, newStock);
```

### IOrderNotificationService

Injected into order processing services:

```csharp
@inject IOrderNotificationService OrderNotifier

// When order status changes:
await OrderNotifier.NotifyOrderStatusChangedAsync(orderId, "Processing");

// When shipping info available:
await OrderNotifier.NotifyTrackingUpdatedAsync(orderId, "1Z123456", "UPS", "https://...");
```

## Configuration

### Program.cs Setup

```csharp
// Add SignalR
builder.Services.AddSignalR();

// Add notification services
builder.Services.AddScoped<ICartNotificationService, CartNotificationService>();
builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();

// Map hubs
app.MapHub<CartHub>("/hubs/cart");
app.MapHub<OrderHub>("/hubs/orders");
app.MapHub<NotificationHub>("/hubs/notifications");
```

## Performance Considerations

### Hub Group Usage
- **Reduces message traffic** by routing to specific customers/orders
- `Order-123` group only receives updates for that order
- `cart-customer-456` group only receives that customer's cart updates

### Automatic Reconnection
```csharp
new HubConnectionBuilder()
    .WithUrl(Nav.ToAbsoluteUri("/hubs/cart"))
    .WithAutomaticReconnect()  // Auto reconnect with exponential backoff
    .Build();
```

Reconnection strategy:
- 0 seconds (immediately)
- 2 seconds
- 10 seconds
- 30 seconds
- After 30 seconds: stop attempting (user can manually reconnect)

### Scalability
For horizontal scaling, consider:
- **SignalR Backplane** (Azure SignalR Service, Redis)
- **Sticky Sessions** (route clients to same server)
- **Database-backed Hub** (for cross-server coordination)

## Error Handling

### Connection Failures
```csharp
try
{
    await _hubConnection.StartAsync();
}
catch (Exception ex)
{
    Logger.LogError(ex, "Failed to connect to cart hub");
    // Continue without real-time - UI still functional
}
```

### Hub Method Failures
```csharp
try
{
    await CartNotifier.NotifyItemAddedAsync(customerId, item);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error sending cart notification");
    // Notification not critical - order still successful
}
```

## Testing

### Manual Testing

1. **Cart Sync**
   - [ ] Add item in tab 1
   - [ ] Check tab 2 - cart updates automatically
   - [ ] Remove item in tab 2
   - [ ] Check tab 1 - cart updates

2. **Order Tracking**
   - [ ] Place order in tab 1
   - [ ] Open order in tab 2
   - [ ] Simulate vendor shipping from admin
   - [ ] Tab 2 receives tracking notification

3. **Connection Loss**
   - [ ] Open cart page
   - [ ] Disconnect network (DevTools → Offline)
   - [ ] Close network (reconnect)
   - [ ] Verify cart updates resume

### Unit Testing

```csharp
[Fact]
public async Task CartNotificationService_NotifyItemAdded_SendsMessageToCartGroup()
{
    // Arrange
    var mockHub = new Mock<IHubContext<CartHub>>();
    var service = new CartNotificationService(mockHub.Object, _logger);
    var customerId = Guid.NewGuid();
    var item = new CartItemViewModel { /* ... */ };

    // Act
    await service.NotifyItemAddedAsync(customerId, item);

    // Assert
    mockHub.Verify(h => h.Clients.Group($"cart-{customerId}")
        .SendCoreAsync("ItemAdded", It.IsAny<object?[]>(), default),
        Times.Once);
}
```

## Future Enhancements

1. **Inventory Management**
   - Real-time low-stock alerts
   - Automatic out-of-stock notifications
   - Restock notifications

2. **Chat & Support**
   - Live customer support chat
   - Real-time agent assignment
   - Typing indicators

3. **Analytics**
   - Live sales dashboard for vendors
   - Real-time order metrics
   - Peak time alerts

4. **Wishlist Sync**
   - Share wishlists in real-time
   - Notify when wishlist item on sale
   - Collaborative shopping

5. **Marketplace Events**
   - Flash sales notifications
   - New product alerts
   - Price drop alerts

## Debugging

### Browser DevTools
1. Open DevTools → Network tab
2. Filter by WebSocket connections
3. Monitor `/hubs/cart` and `/hubs/orders` connections
4. Watch for connection open/close events

### Server Logging
```csharp
// Enable SignalR debug logging
builder.Services.AddSignalR().AddHubOptions<CartHub>(options =>
{
    options.MaximumReceiveMessageSize = 64 * 1024; // 64 KB max message
});

// In appsettings.json:
"Logging": {
    "LogLevel": {
        "Microsoft.AspNetCore.SignalR": "Debug"
    }
}
```

## Related Features

- [Cart State Management](./State/Cart/)
- [Order Repository](./Repositories/IOrderRepository.cs)
- [Notification Service](./Services/NotificationService.cs)
- [Testing Guide](./TESTING_GUIDE.md)
