using HalalChain.Application.Catalog.Queries;
using HalalChain.Application.Common.Interfaces;
using HalalChain.Platform.Contracts.Catalog.Dto;
using MediatR;

namespace HalalChain.Application.Catalog.Handlers;

internal sealed class GetEnrichedProductHandler(IProductRepository productRepository)
    : IRequestHandler<GetEnrichedProductQuery, EnrichedProductDto?>
{
    public async Task<EnrichedProductDto?> Handle(GetEnrichedProductQuery request, CancellationToken ct)
    {
        return await productRepository.GetEnrichedAsync(request.Id, ct);
    }
}
