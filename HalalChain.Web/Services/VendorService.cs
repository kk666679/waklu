using HalalChain.Models;

namespace HalalChain.Services;

public class VendorService : IVendorService
{
    private readonly List<Vendor> _vendors = new();
    private int _nextId = 1;

    public VendorService()
    {
        _vendors.Add(new Vendor { Id = _nextId++, Name = "Saffron Foods", Email = "hello@saffron.test", IsVerified = true });
        _vendors.Add(new Vendor { Id = _nextId++, Name = "Dates & Co", Email = "team@dates.test", IsVerified = true });
        _vendors.Add(new Vendor { Id = _nextId++, Name = "Al-Madina Spices", Email = "info@almadina.test", IsVerified = false });
    }

    public Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Vendor>>(_vendors.ToList());

    public Task<Vendor?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_vendors.FirstOrDefault(v => v.Id == id));

    public Task<Vendor> CreateAsync(Vendor vendor, CancellationToken ct = default)
    {
        vendor.Id = _nextId++;
        _vendors.Add(vendor);
        return Task.FromResult(vendor);
    }

    public Task<Vendor> UpdateAsync(Vendor vendor, CancellationToken ct = default)
    {
        var existing = _vendors.FirstOrDefault(v => v.Id == vendor.Id);
        if (existing is null) return Task.FromResult(vendor);
        existing.Name = vendor.Name;
        existing.Email = vendor.Email;
        existing.Phone = vendor.Phone;
        existing.IsActive = vendor.IsActive;
        existing.IsVerified = vendor.IsVerified;
        return Task.FromResult(existing);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _vendors.RemoveAll(v => v.Id == id);
        return Task.CompletedTask;
    }
}
