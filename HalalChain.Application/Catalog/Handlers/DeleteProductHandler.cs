using HalalChain.Application.Catalog.Commands;
using HalalChain.Application.Common.Interfaces;
using MediatR;

namespace HalalChain.Application.Catalog.Handlers;

internal sealed class DeleteProductHandler(IProductRepository productRepository)
    : IRequestHandler<DeleteProductCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        await productRepository.DeleteAsync(request.Id, ct);
        return Unit.Value;
    }
}
