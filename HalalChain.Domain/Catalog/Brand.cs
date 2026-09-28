namespace HalalChain.Domain.Catalog;

/// <summary>
/// A product brand. Brands are first-class so they can have their own page,
/// halal profile aggregation, and verification state.
/// </summary>
public sealed class Brand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? WebsiteUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
