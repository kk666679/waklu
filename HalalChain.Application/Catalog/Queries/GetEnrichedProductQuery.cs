using HalalChain.Platform.Contracts.Catalog.Dto;
using MediatR;

namespace HalalChain.Application.Catalog.Queries;

public record GetEnrichedProductQuery(Guid Id)
    : IRequest<EnrichedProductDto?>;
