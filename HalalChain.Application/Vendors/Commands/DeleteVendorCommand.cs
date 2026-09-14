using MediatR;

namespace HalalChain.Application.Vendors.Commands;

public record DeleteVendorCommand(Guid Id)
    : IRequest<Unit>;
