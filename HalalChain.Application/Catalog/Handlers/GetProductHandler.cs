using HalalChain.Application.Catalog.Queries;
using HalalChain.Application.Common.Interfaces;
using HalalChain.Platform.Contracts.Catalog.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Application.Catalog.Handlers;

internal sealed class GetProductHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductQuery request, CancellationToken ct)
    {
        return await productRepository.GetByIdAsync(request.Id, ct);
    }
}
