using HalalChain.Platform.Contracts.Vendors.Requests;
using MediatR;

namespace HalalChain.Application.Vendors.Commands;

public record UpdateVendorCommand(Guid Id, UpdateVendorRequest Request)
    : IRequest<Unit>;
