using HalalChain.Models;

namespace HalalChain.Services;

public class PayoutService : IPayoutService
{
    private readonly List<Payout> _payouts = new();
    private int _nextId = 1;

    public PayoutService()
    {
        _payouts.Add(new Payout { Id = _nextId++, VendorId = 1, Amount = 250m, Status = PayoutStatus.Pending });
        _payouts.Add(new Payout { Id = _nextId++, VendorId = 2, Amount = 480m, Status = PayoutStatus.Approved });
    }

    public Task<IReadOnlyList<Payout>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Payout>>(_payouts.ToList());

    public Task<IReadOnlyList<Payout>> GetForVendorAsync(int vendorId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Payout>>(_payouts.Where(p => p.VendorId == vendorId).ToList());

    public Task<Payout> RequestAsync(Payout payout, CancellationToken ct = default)
    {
        payout.Id = _nextId++;
        payout.Status = PayoutStatus.Pending;
        _payouts.Add(payout);
        return Task.FromResult(payout);
    }

    public Task<Payout> UpdateStatusAsync(int id, PayoutStatus status, CancellationToken ct = default)
    {
        var existing = _payouts.FirstOrDefault(p => p.Id == id);
        if (existing is null) return Task.FromResult(new Payout { Id = id });
        existing.Status = status;
        if (status is PayoutStatus.Paid or PayoutStatus.Rejected or PayoutStatus.Approved)
            existing.ProcessedAt = DateTime.UtcNow;
        return Task.FromResult(existing);
    }
}
