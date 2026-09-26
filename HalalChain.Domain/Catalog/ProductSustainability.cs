namespace HalalChain.Domain.Catalog;

/// <summary>
/// Environmental and sustainability metrics for a product.
/// Tracks carbon footprint, certifications, and ecological impact.
/// Domain entity for sustainability tracking.
/// </summary>
public sealed class ProductSustainability
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>
    /// Carbon footprint in kg CO2 equivalent per unit.
    /// </summary>
    public decimal? CarbonFootprintKgCo2 { get; set; }

    /// <summary>
    /// Eco-rating on a scale of 1-5 (5 = most eco-friendly).
    /// </summary>
    public int? EcoRating { get; set; }

    /// <summary>
    /// Sustainability certifications (Fair Trade, Organic, B Corp, etc.).
    /// </summary>
    public List<string> Certifications { get; set; } = [];

    /// <summary>
    /// Packaging material type (Recyclable Plastic, Compostable, Glass, etc.).
    /// </summary>
    public string? PackagingMaterial { get; set; }

    /// <summary>
    /// Percentage of recyclable content in packaging (0-100).
    /// </summary>
    public int? RecyclablePercentage { get; set; }

    /// <summary>
    /// Energy used in production (kWh per unit).
    /// </summary>
    public decimal? ProductionEnergyKwh { get; set; }

    /// <summary>
    /// Water used in production (liters per unit).
    /// </summary>
    public decimal? ProductionWaterLiters { get; set; }

    /// <summary>
    /// Country/Region of origin for supply chain transparency.
    /// </summary>
    public string? OriginCountry { get; set; }

    /// <summary>
    /// ISO 14001 Environmental Management certification.
    /// </summary>
    public bool HasIso14001Certification { get; set; }

    /// <summary>
    /// Fair trade compliance flag.
    /// </summary>
    public bool IsFairTrade { get; set; }

    /// <summary>
    /// Whether the product is vegan/plant-based.
    /// </summary>
    public bool IsVegan { get; set; }

    /// <summary>
    /// URL to sustainability report or certification document.
    /// </summary>
    public string? SustainabilityReportUrl { get; set; }

    /// <summary>
    /// Last updated timestamp for sustainability data.
    /// </summary>
    public DateTimeOffset? LastUpdatedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
