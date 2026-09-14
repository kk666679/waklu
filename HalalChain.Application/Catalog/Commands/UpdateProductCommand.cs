using HalalChain.Platform.Contracts.Catalog.Requests;
using MediatR;

namespace HalalChain.Application.Catalog.Commands;

public record UpdateProductCommand(Guid Id, UpdateProductRequest Request, Guid VendorId, bool IsAdmin)
    : IRequest<Unit>;
