using HalalChain.Application.Vendors.Queries;
using HalalChain.Application.Common.Interfaces;
using HalalChain.Platform.Contracts.Vendors.Dto;
using MediatR;

namespace HalalChain.Application.Vendors.Handlers;

internal sealed class ListVendorsHandler(IVendorRepository vendorRepository)
    : IRequestHandler<ListVendorsQuery, VendorDto[]>
{
    public async Task<VendorDto[]> Handle(ListVendorsQuery request, CancellationToken ct)
    {
        return await vendorRepository.GetAllAsync(ct);
    }
}
