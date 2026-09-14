using HalalChain.Application.Catalog.Commands;
using HalalChain.Application.Common.Exceptions;
using HalalChain.Application.Common.Interfaces;
using MediatR;

namespace HalalChain.Application.Catalog.Handlers;

internal sealed class UpdateProductHandler(IProductRepository productRepository)
    : IRequestHandler<UpdateProductCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        await productRepository.UpdateAsync(request.Id, request.Request, request.VendorId, request.IsAdmin, ct);
        return Unit.Value;
    }
}
