using MediatR;

namespace HalalChain.Application.Catalog.Queries;

/// <summary>
/// Query to get comprehensive vendor analytics.
/// Includes top performers, conversion funnels, return analysis, and more.
/// </summary>
public record GetVendorPerformanceAnalyticsQuery : IRequest<VendorPerformanceAnalytics>
{
    public Guid VendorId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
}

public record VendorPerformanceAnalytics
{
    public Guid VendorId { get; set; }
    public DateTimeOffset ReportedAt { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalUnitsSold { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal ReturnRate { get; set; }
    public List<TopPerformingProduct> TopProducts { get; set; } = [];
    public List<CategoryPerformance> CategoryPerformance { get; set; } = [];
    public ConversionFunnelData ConversionFunnel { get; set; } = new();
    public Dictionary<string, decimal> KeyMetrics { get; set; } = [];
}

public record TopPerformingProduct
{
    public Guid ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int UnitsSold { get; set; }
    public decimal ConversionRate { get; set; }
    public int Rank { get; set; }
}

public record CategoryPerformance
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int UnitsSold { get; set; }
    public decimal PercentOfTotal { get; set; }
}

public record ConversionFunnelData
{
    public int PageViews { get; set; }
    public int ProductViews { get; set; }
    public int AddToCarts { get; set; }
    public int Checkouts { get; set; }
    public int Orders { get; set; }
    public decimal Overall { get; set; } // Orders / PageViews
}
