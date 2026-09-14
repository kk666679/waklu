using HalalChain.Models;

namespace HalalChain.Services;

public interface IInvoiceService
{
    Task<IReadOnlyList<Invoice>> GetAllAsync(CancellationToken ct = default);
    Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Invoice> CreateAsync(Invoice invoice, CancellationToken ct = default);
    Task<Invoice> MarkPaidAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
