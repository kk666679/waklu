namespace HalalChain.Domain.Vendors;

public sealed class Vendor
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Active, Suspended
    public string? Country { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Catalog.Product> Products { get; set; } = [];
    public List<Commerce.VendorOrder> VendorOrders { get; set; } = [];
}
