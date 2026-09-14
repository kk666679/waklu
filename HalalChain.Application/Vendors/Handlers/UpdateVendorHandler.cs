using HalalChain.Application.Vendors.Commands;
using HalalChain.Application.Common.Exceptions;
using HalalChain.Application.Common.Interfaces;
using HalalChain.Platform.Contracts.Vendors.Dto;
using MediatR;

namespace HalalChain.Application.Vendors.Handlers;

internal sealed class UpdateVendorHandler(IVendorRepository vendorRepository)
    : IRequestHandler<UpdateVendorCommand, Unit>
{
    public async Task<Unit> Handle(UpdateVendorCommand request, CancellationToken ct)
    {
        await vendorRepository.UpdateAsync(request.Id, request.Request, ct);
        return Unit.Value;
    }
}
