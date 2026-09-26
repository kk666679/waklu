namespace HalalChain.Platform.Contracts.Catalog;

/// <summary>
/// Environmental and sustainability metrics for a product.
/// Tracks carbon footprint, certifications, and ecological impact.
/// </summary>
public sealed class SustainabilityDto
{
    /// <summary>
    /// Carbon footprint in kg CO2 equivalent per unit.
    /// Null if not measured or calculated.
    /// </summary>
    public decimal? CarbonFootprintKgCo2 { get; set; }

    /// <summary>
    /// Eco-rating on a scale of 1-5, where 5 is most eco-friendly.
    /// Calculated from certifications, packaging, sourcing, and carbon metrics.
    /// </summary>
    public int? EcoRating { get; set; }

    /// <summary>
    /// List of sustainability certifications (e.g., "Fair Trade", "Organic", "B Corp", "Carbon Neutral").
    /// </summary>
    public List<string> Certifications { get; set; } = [];

    /// <summary>
    /// Packaging material type (e.g., "Recyclable Plastic", "Compostable", "Glass", "Metal", "Paper").
    /// </summary>
    public string? PackagingMaterial { get; set; }

    /// <summary>
    /// Percentage of recyclable content in packaging (0-100).
    /// </summary>
    public int? RecyclablePercentage { get; set; }

    /// <summary>
    /// Energy used in production (kWh per unit). Null if not tracked.
    /// </summary>
    public decimal? ProductionEnergyKwh { get; set; }

    /// <summary>
    /// Water used in production (liters per unit). Null if not tracked.
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
    /// URL to the product's sustainability report or certification document.
    /// </summary>
    public string? SustainabilityReportUrl { get; set; }

    /// <summary>
    /// Last updated timestamp for sustainability data.
    /// </summary>
    public DateTime? LastUpdatedAt { get; set; }
}
