using HalalChain.Application.Catalog.Queries;
using HalalChain.Application.Common.Interfaces;
using HalalChain.Platform.Contracts.Catalog.Dto;
using MediatR;

namespace HalalChain.Application.Catalog.Handlers;

internal sealed class ListProductsHandler(IProductRepository productRepository)
    : IRequestHandler<ListProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(ListProductsQuery request, CancellationToken ct)
    {
        return await productRepository.ListAsync(
            request.Category,
            request.HalalStatus,
            request.MinPrice,
            request.MaxPrice,
            request.Vendor,
            request.Search,
            request.SortBy,
            request.SortOrder,
            request.Page,
            request.PageSize,
            ct);
    }
}
