using HalalChain.Models;

namespace HalalChain.Services;

public interface IPayoutService
{
    Task<IReadOnlyList<Payout>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Payout>> GetForVendorAsync(int vendorId, CancellationToken ct = default);
    Task<Payout> RequestAsync(Payout payout, CancellationToken ct = default);
    Task<Payout> UpdateStatusAsync(int id, PayoutStatus status, CancellationToken ct = default);
}
