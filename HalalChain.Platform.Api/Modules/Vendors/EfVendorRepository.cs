using System.Linq.Expressions;
using HalalChain.Application.Vendors.Interfaces;
using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Contracts.Vendors.Dto;
using HalalChain.Platform.Contracts.Vendors.Requests;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Vendors;

public sealed class EfVendorRepository(HalalChainDbContext db) : IVendorRepository
{
    public async Task<VendorDto> CreateAsync(RegisterVendorRequest request, CancellationToken ct = default)
    {
        var vendor = new Domain.Vendors.Vendor
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Name.Trim().ToLowerInvariant().Replace(' ', '-'),
            Country = request.Country
        };

        db.Vendors.Add(vendor);
        await db.SaveChangesAsync(ct);
        return ToDto(vendor);
    }

    public async Task UpdateAsync(Guid id, UpdateVendorRequest request, CancellationToken ct = default)
    {
        var vendor = await db.Vendors.FindAsync([id], ct)
            ?? throw new Application.Common.Exceptions.NotFoundException("Vendor", id);

        if (request.Name is not null)
        {
            vendor.Name = request.Name;
            vendor.Slug = request.Name.Trim().ToLowerInvariant().Replace(' ', '-');
        }

        if (request.Country is not null) vendor.Country = request.Country;
        if (request.Status is not null) vendor.Status = request.Status;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var vendor = await db.Vendors.FindAsync([id], ct)
            ?? throw new Application.Common.Exceptions.NotFoundException("Vendor", id);
        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync(ct);
    }

    public async Task<VendorDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Vendors.AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new VendorDto
            {
                Id = v.Id,
                Name = v.Name,
                Slug = v.Slug,
                Status = v.Status,
                Country = v.Country,
                CreatedAt = v.CreatedAt
            })
            .SingleOrDefaultAsync(ct);

    public async Task<VendorDto[]> GetAllAsync(CancellationToken ct = default) =>
        await db.Vendors.AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new VendorDto
            {
                Id = v.Id,
                Name = v.Name,
                Slug = v.Slug,
                Status = v.Status,
                Country = v.Country,
                CreatedAt = v.CreatedAt
            })
            .ToArrayAsync(ct);

    private static VendorDto ToDto(Domain.Vendors.Vendor vendor) => new()
    {
        Id = vendor.Id,
        Name = vendor.Name,
        Slug = vendor.Slug,
        Status = vendor.Status,
        Country = vendor.Country,
        CreatedAt = vendor.CreatedAt
    };
}
