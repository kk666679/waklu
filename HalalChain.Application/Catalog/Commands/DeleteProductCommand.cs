using MediatR;

namespace HalalChain.Application.Catalog.Commands;

public record DeleteProductCommand(Guid Id)
    : IRequest<Unit>;
