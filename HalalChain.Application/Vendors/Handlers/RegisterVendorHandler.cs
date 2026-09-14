using HalalChain.Application.Vendors.Commands;
using HalalChain.Application.Common.Exceptions;
using HalalChain.Application.Common.Interfaces;
using HalalChain.Platform.Contracts.Vendors.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Application.Vendors.Handlers;

internal sealed class RegisterVendorHandler(IVendorRepository vendorRepository)
    : IRequestHandler<RegisterVendorCommand, Guid>
{
    public async Task<Guid> Handle(RegisterVendorCommand request, CancellationToken ct)
    {
        var vendor = await vendorRepository.CreateAsync(request.Request, ct);
        return vendor.Id;
    }
}
