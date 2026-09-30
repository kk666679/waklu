namespace HalalChain.Marketplace.Models.ViewModels;

/// <summary>ViewModel for displaying an Order.</summary>
public class OrderViewModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Paid, Shipped, Delivered, Cancelled
    public decimal Total { get; set; }
    public string Currency { get; set; } = "MYR";
    public string? ShippingAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Vendor orders breakdown
    public List<VendorOrderViewModel> VendorOrders { get; set; } = [];

    public decimal SubTotal => VendorOrders.Sum(vo => vo.Subtotal);
    public int ItemCount => VendorOrders.Sum(vo => vo.ItemCount);
}

/// <summary>ViewModel for displaying a Vendor Order (order split by vendor).</summary>
public class VendorOrderViewModel
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public string Currency { get; set; } = "MYR";
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    // Items in this vendor order
    public List<OrderItemViewModel> Items { get; set; } = [];

    public int ItemCount => Items.Sum(i => i.Quantity);
}

/// <summary>ViewModel for displaying an Order Line Item.</summary>
public class OrderItemViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "MYR";
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>ViewModel for creating an Order (checkout).</summary>
public class CreateOrderViewModel
{
    public Guid CustomerId { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? PaymentToken { get; set; }
    public List<CartItemToOrder> CartItems { get; set; } = [];
}

public class CartItemToOrder
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

/// <summary>ViewModel for displaying order history (summary view).</summary>
public class OrderHistoryViewModel
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Currency { get; set; } = "MYR";
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DisplayDate => CreatedAt.ToString("MMM dd, yyyy");
    public string StatusBadgeClass => Status switch
    {
        "Pending" => "badge bg-warning",
        "Paid" => "badge bg-info",
        "Shipped" => "badge bg-primary",
        "Delivered" => "badge bg-success",
        "Cancelled" => "badge bg-danger",
        _ => "badge bg-secondary"
    };
}
