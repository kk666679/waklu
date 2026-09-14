using HalalChain.Models;

namespace HalalChain.Services;

public interface IPromotionService
{
    Task<IReadOnlyList<Coupon>> GetAllAsync(CancellationToken ct = default);
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Coupon> CreateAsync(Coupon coupon, CancellationToken ct = default);
    Task<Coupon> UpdateAsync(Coupon coupon, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    decimal CalculateDiscount(Coupon coupon, decimal subTotal);
}
