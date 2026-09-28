namespace HalalChain.Domain.Catalog;

public sealed class ProductType
{
    public Guid Id { get; set; }
    public Guid SubcategoryId { get; set; }
    public Subcategory Subcategory { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string PathSlug { get; set; } = string.Empty;
}
