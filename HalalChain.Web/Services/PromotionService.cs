using HalalChain.Models;

namespace HalalChain.Services;

public class PromotionService : IPromotionService
{
    private readonly List<Coupon> _coupons = new();
    private int _nextId = 1;

    public PromotionService()
    {
        _coupons.Add(new Coupon { Id = _nextId++, Code = "HALAL10", Type = CouponType.Percentage, Value = 10, ExpiresAt = DateTime.UtcNow.AddMonths(3) });
        _coupons.Add(new Coupon { Id = _nextId++, Code = "SAVE5", Type = CouponType.FixedAmount, Value = 5, MinOrderAmount = 30, ExpiresAt = DateTime.UtcNow.AddMonths(1) });
    }

    public Task<IReadOnlyList<Coupon>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Coupon>>(_coupons.ToList());

    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default)
        => Task.FromResult(_coupons.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));

    public Task<Coupon> CreateAsync(Coupon coupon, CancellationToken ct = default)
    {
        coupon.Id = _nextId++;
        _coupons.Add(coupon);
        return Task.FromResult(coupon);
    }

    public Task<Coupon> UpdateAsync(Coupon coupon, CancellationToken ct = default)
    {
        var existing = _coupons.FirstOrDefault(c => c.Id == coupon.Id);
        if (existing is null) return Task.FromResult(coupon);
        existing.Code = coupon.Code;
        existing.Value = coupon.Value;
        existing.Type = coupon.Type;
        existing.ExpiresAt = coupon.ExpiresAt;
        existing.IsActive = coupon.IsActive;
        return Task.FromResult(existing);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _coupons.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }

    public decimal CalculateDiscount(Coupon coupon, decimal subTotal)
    {
        if (coupon.MinOrderAmount.HasValue && subTotal < coupon.MinOrderAmount.Value) return 0m;
        var discount = coupon.Type switch
        {
            CouponType.Percentage => subTotal * (coupon.Value / 100m),
            CouponType.FixedAmount => coupon.Value,
            _ => 0m
        };
        if (coupon.MaxDiscount.HasValue) discount = Math.Min(discount, coupon.MaxDiscount.Value);
        return Math.Min(discount, subTotal);
    }
}
