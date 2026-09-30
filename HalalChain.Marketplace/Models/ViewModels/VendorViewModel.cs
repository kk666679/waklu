namespace HalalChain.Marketplace.Models.ViewModels;

/// <summary>ViewModel for displaying a Vendor (merchant).</summary>
public class VendorViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pending, Active, Suspended
    public string? Country { get; set; }
    public int ProductCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsActive => Status == "Active";
    public string StatusBadgeClass => Status switch
    {
        "Active" => "badge bg-success",
        "Pending" => "badge bg-warning",
        "Suspended" => "badge bg-danger",
        _ => "badge bg-secondary"
    };
}

/// <summary>ViewModel for vendor marketplace display (summary).</summary>
public class VendorProfileViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Country { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int ProductCount { get; set; }
    public List<ProductSummaryViewModel> FeaturedProducts { get; set; } = [];
}
