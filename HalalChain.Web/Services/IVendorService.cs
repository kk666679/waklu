using HalalChain.Models;

namespace HalalChain.Services;

public interface IVendorService
{
    Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default);
    Task<Vendor?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Vendor> CreateAsync(Vendor vendor, CancellationToken ct = default);
    Task<Vendor> UpdateAsync(Vendor vendor, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
