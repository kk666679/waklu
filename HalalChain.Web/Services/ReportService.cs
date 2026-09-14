using HalalChain.Models;

namespace HalalChain.Services;

public class ReportService : IReportService
{
    public Task<ReportData> GetSalesReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rng = new Random(7);
        var days = (to - from).Days;
        var points = Enumerable.Range(0, Math.Max(1, days))
            .Select(i => new ReportPoint { Date = from.AddDays(i), Value = 200m + (decimal)rng.NextDouble() * 800m, Count = rng.Next(5, 30) })
            .ToList();
        return Task.FromResult(new ReportData
        {
            Title = "Sales Report",
            From = from,
            To = to,
            Points = points,
            Total = points.Sum(p => p.Value)
        });
    }

    public Task<ReportData> GetProductsReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return Task.FromResult(new ReportData
        {
            Title = "Top Products",
            From = from,
            To = to,
            Total = 12500m,
            Breakdowns = new() { ["Saffron 50g"] = 4200m, ["Medjool Dates 1kg"] = 3500m, ["Halal Ghee"] = 2800m, ["Sumac"] = 2000m }
        });
    }

    public Task<ReportData> GetVendorsReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return Task.FromResult(new ReportData
        {
            Title = "Top Vendors",
            From = from,
            To = to,
            Total = 18500m,
            Breakdowns = new() { ["Saffron Foods"] = 7800m, ["Dates & Co"] = 6200m, ["Al-Madina Spices"] = 4500m }
        });
    }
}
