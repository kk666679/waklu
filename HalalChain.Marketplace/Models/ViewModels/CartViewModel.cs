namespace HalalChain.Marketplace.Models.ViewModels;

/// <summary>ViewModel for displaying Cart Item.</summary>
public class CartItemViewModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MYR";
    public int Quantity { get; set; }
    public decimal LineTotal => Price * Quantity;
    public bool IsInStock { get; set; } = true;
    public string VendorName { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}

/// <summary>ViewModel for displaying Cart summary.</summary>
public class CartSummaryViewModel
{
    public List<CartItemViewModel> Items { get; set; } = [];
    public string Currency { get; set; } = "MYR";
    
    public int ItemCount => Items.Sum(i => i.Quantity);
    public int UniqueProductCount => Items.Count;
    public decimal SubTotal => Items.Sum(i => i.LineTotal);
    public decimal TaxAmount => SubTotal * 0.06m; // Example: 6% tax
    public decimal ShippingAmount => Items.Any() ? 10m : 0m; // Example: $10 flat rate
    public decimal Total => SubTotal + TaxAmount + ShippingAmount;
    
    public bool IsEmpty => !Items.Any();
    public bool HasOutOfStockItems => Items.Any(i => !i.IsInStock);
}

/// <summary>ViewModel for displaying Wishlist Item.</summary>
public class WishlistItemViewModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MYR";
    public bool IsInStock { get; set; } = true;
    public string VendorName { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}

/// <summary>ViewModel for displaying Wishlist summary.</summary>
public class WishlistSummaryViewModel
{
    public List<WishlistItemViewModel> Items { get; set; } = [];
    public string Currency { get; set; } = "MYR";
    
    public int ItemCount => Items.Count;
    public decimal TotalValue => Items.Sum(i => i.Price);
    public bool IsEmpty => !Items.Any();
    public int OutOfStockCount => Items.Count(i => !i.IsInStock);
}
