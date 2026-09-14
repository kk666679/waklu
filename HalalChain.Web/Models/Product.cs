namespace HalalChain.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public int VendorId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHalal { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Category? Category { get; set; }
    public Vendor? Vendor { get; set; }
    public HalalCertification? Certification { get; set; }
}
