using HalalChain.Platform.Contracts.Vendors.Requests;
using MediatR;

namespace HalalChain.Application.Vendors.Commands;

public record RegisterVendorCommand(RegisterVendorRequest Request)
    : IRequest<Guid>;
