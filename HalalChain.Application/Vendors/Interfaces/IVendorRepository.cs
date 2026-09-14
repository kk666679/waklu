using HalalChain.Application.Common.Interfaces;

namespace HalalChain.Application.Vendors.Interfaces;

public interface IVendorRepository
{
    Task<HalalChain.Platform.Contracts.Vendors.Dto.VendorDto> CreateAsync(HalalChain.Platform.Contracts.Vendors.Requests.RegisterVendorRequest request, CancellationToken ct = default);
    Task UpdateAsync(Guid id, HalalChain.Platform.Contracts.Vendors.Requests.UpdateVendorRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<HalalChain.Platform.Contracts.Vendors.Dto.VendorDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<HalalChain.Platform.Contracts.Vendors.Dto.VendorDto[]> GetAllAsync(CancellationToken ct = default);
}
