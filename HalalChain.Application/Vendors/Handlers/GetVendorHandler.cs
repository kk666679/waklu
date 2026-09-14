using HalalChain.Application.Vendors.Queries;
using HalalChain.Application.Common.Interfaces;
using HalalChain.Platform.Contracts.Vendors.Dto;
using MediatR;

namespace HalalChain.Application.Vendors.Handlers;

internal sealed class GetVendorHandler(IVendorRepository vendorRepository)
    : IRequestHandler<GetVendorQuery, VendorDto?>
{
    public async Task<VendorDto?> Handle(GetVendorQuery request, CancellationToken ct)
    {
        return await vendorRepository.GetByIdAsync(request.Id, ct);
    }
}
