using HalalChain.Application.Vendors.Commands;
using HalalChain.Application.Common.Interfaces;
using MediatR;

namespace HalalChain.Application.Vendors.Handlers;

internal sealed class DeleteVendorHandler(IVendorRepository vendorRepository)
    : IRequestHandler<DeleteVendorCommand, Unit>
{
    public async Task<Unit> Handle(DeleteVendorCommand request, CancellationToken ct)
    {
        await vendorRepository.DeleteAsync(request.Id, ct);
        return Unit.Value;
    }
}
