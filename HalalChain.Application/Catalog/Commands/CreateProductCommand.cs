using HalalChain.Platform.Contracts.Catalog.Requests;
using MediatR;

namespace HalalChain.Application.Catalog.Commands;

public record CreateProductCommand(CreateProductRequest Request, Guid VendorId)
    : IRequest<Guid>;
