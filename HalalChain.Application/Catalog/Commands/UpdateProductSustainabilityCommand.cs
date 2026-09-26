using MediatR;

namespace HalalChain.Application.Catalog.Commands;

/// <summary>
/// Command to update product sustainability metrics.
/// </summary>
public record UpdateProductSustainabilityCommand : IRequest<Unit>
{
    public Guid ProductId { get; set; }
    public Guid VendorId { get; set; }
    public decimal? CarbonFootprintKgCo2 { get; set; }
    public int? EcoRating { get; set; } // 1-5
    public List<string> Certifications { get; set; } = []; // Fair Trade, Organic, B Corp, etc.
    public string? PackagingMaterial { get; set; }
    public int? RecyclablePercentage { get; set; }
    public decimal? ProductionEnergyKwh { get; set; }
    public decimal? ProductionWaterLiters { get; set; }
    public string? OriginCountry { get; set; }
    public bool HasIso14001Certification { get; set; }
    public bool IsFairTrade { get; set; }
    public bool IsVegan { get; set; }
    public string? SustainabilityReportUrl { get; set; }
}
