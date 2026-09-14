using HalalChain.Platform.Contracts.Catalog.Dto;
using MediatR;

namespace HalalChain.Application.Catalog.Queries;

public record GetProductQuery(Guid Id)
    : IRequest<ProductDto?>;
