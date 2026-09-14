using HalalChain.Platform.Contracts.Catalog.Dto;
using MediatR;

namespace HalalChain.Application.Catalog.Queries;

public record ListProductsQuery(
    string? Category,
    string? HalalStatus,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Vendor,
    string? Search,
    string? SortBy,
    string? SortOrder,
    int Page = 1,
    int PageSize = 20)
    : IRequest<PagedResult<ProductDto>>;
