using HalalChain.Marketplace.Models.ViewModels;
using HalalChain.Marketplace.Realtime;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace HalalChain.Marketplace.Tests.Services;

/// <summary>Unit tests for real-time notification services.</summary>
public class CartNotificationServiceTests
{
    private readonly Mock<IHubContext<CartHub>> _mockHubContext;
    private readonly Mock<IClientProxy> _mockClients;
    private readonly Mock<ILogger<CartNotificationService>> _mockLogger;
    private readonly CartNotificationService _notificationService;

    public CartNotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<CartHub>>();
        _mockClients = new Mock<IClientProxy>();
        _mockLogger = new Mock<ILogger<CartNotificationService>>();

        _mockHubContext.Setup(h => h.Clients.Group(It.IsAny<string>()))
            .Returns(_mockClients.Object);
        _mockHubContext.Setup(h => h.Clients.All)
            .Returns(_mockClients.Object);

        _notificationService = new CartNotificationService(_mockHubContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task NotifyItemAddedAsync_SendsItemAddedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var item = new CartItemViewModel { Id = Guid.NewGuid(), Quantity = 1 };

        // Act
        await _notificationService.NotifyItemAddedAsync(customerId, item);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "ItemAdded",
            It.Is<object?[]>(o => o.Length == 1 && o[0] is CartItemViewModel),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyItemRemovedAsync_SendsItemRemovedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Act
        await _notificationService.NotifyItemRemovedAsync(customerId, itemId);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "ItemRemoved",
            It.Is<object?[]>(o => o.Length == 1 && o[0] is Guid),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyCartUpdatedAsync_SendsCartUpdatedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var cartSummary = new CartSummaryViewModel
        {
            ItemCount = 3,
            SubtotalAmount = 100m,
            TotalAmount = 110m
        };

        // Act
        await _notificationService.NotifyCartUpdatedAsync(customerId, cartSummary);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "CartUpdated",
            It.Is<object?[]>(o => o.Length == 1 && o[0] is CartSummaryViewModel),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyCartClearedAsync_SendsCartClearedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        await _notificationService.NotifyCartClearedAsync(customerId);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "CartCleared",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyStockChangedAsync_SendsToAllClients()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var newStock = 50;

        // Act
        await _notificationService.NotifyStockChangedAsync(productId, newStock);

        // Assert
        _mockHubContext.Verify(h => h.Clients.All, Times.Once);
        _mockClients.Verify(c => c.SendCoreAsync(
            "StockChanged",
            It.Is<object?[]>(o => o.Length == 2),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyOutOfStockAsync_SendsOutOfStockEvent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "Test Product";

        // Act
        await _notificationService.NotifyOutOfStockAsync(productId, productName);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "ProductOutOfStock",
            It.Is<object?[]>(o => o.Length == 2),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyItemAddedAsync_WithException_DoesNotThrow()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var item = new CartItemViewModel { Id = Guid.NewGuid(), Quantity = 1 };

        _mockClients.Setup(c => c.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Hub error"));

        // Act & Assert
        await _notificationService.NotifyItemAddedAsync(customerId, item); // Should not throw
    }
}

/// <summary>Unit tests for OrderNotificationService.</summary>
public class OrderNotificationServiceTests
{
    private readonly Mock<IHubContext<OrderHub>> _mockHubContext;
    private readonly Mock<IClientProxy> _mockClients;
    private readonly Mock<ILogger<OrderNotificationService>> _mockLogger;
    private readonly OrderNotificationService _notificationService;

    public OrderNotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<OrderHub>>();
        _mockClients = new Mock<IClientProxy>();
        _mockLogger = new Mock<ILogger<OrderNotificationService>>();

        _mockHubContext.Setup(h => h.Clients.Group(It.IsAny<string>()))
            .Returns(_mockClients.Object);

        _notificationService = new OrderNotificationService(_mockHubContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task NotifyOrderStatusChangedAsync_SendsStatusChangeEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var newStatus = "Shipped";

        // Act
        await _notificationService.NotifyOrderStatusChangedAsync(orderId, newStatus);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "OrderStatusChanged",
            It.Is<object?[]>(o => o.Length == 3),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyVendorOrderUpdatedAsync_SendsVendorUpdateEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var vendorName = "Test Vendor";
        var status = "Processing";

        // Act
        await _notificationService.NotifyVendorOrderUpdatedAsync(orderId, vendorName, status);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "VendorOrderUpdated",
            It.Is<object?[]>(o => o.Length == 2),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyTrackingUpdatedAsync_SendsTrackingEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var trackingNumber = "1Z123456";
        var carrierName = "UPS";
        var trackingUrl = "https://tracking.ups.com";

        // Act
        await _notificationService.NotifyTrackingUpdatedAsync(orderId, trackingNumber, carrierName, trackingUrl);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "TrackingUpdated",
            It.Is<object?[]>(o => o.Length == 3),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyOrderDeliveredAsync_SendsDeliveryEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerName = "John Doe";

        // Act
        await _notificationService.NotifyOrderDeliveredAsync(orderId, customerName);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "OrderDelivered",
            It.Is<object?[]>(o => o.Length == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyOrderPlacedAsync_SendsOrderPlacedEvent()
    {
        // Arrange
        var vendorId = Guid.NewGuid();
        var order = new HalalChain.Marketplace.Models.ViewModels.OrderViewModel
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001"
        };

        // Act
        await _notificationService.NotifyOrderPlacedAsync(vendorId, order);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "OrderPlaced",
            It.Is<object?[]>(o => o.Length == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyOrderStatusChangedAsync_WithReason_IncludesReason()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var newStatus = "Cancelled";
        var reason = "Customer request";

        // Act
        await _notificationService.NotifyOrderStatusChangedAsync(orderId, newStatus, reason);

        // Assert
        _mockClients.Verify(c => c.SendCoreAsync(
            "OrderStatusChanged",
            It.Is<object?[]>(o => o.Length == 3 && o[2]?.Equals(reason) == true),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
