using HalalChain.Models;

namespace HalalChain.Services;

public interface IReportService
{
    Task<ReportData> GetSalesReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<ReportData> GetProductsReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<ReportData> GetVendorsReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
