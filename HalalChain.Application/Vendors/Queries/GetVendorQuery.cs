using HalalChain.Platform.Contracts.Vendors.Dto;
using MediatR;

namespace HalalChain.Application.Vendors.Queries;

public record GetVendorQuery(Guid Id)
    : IRequest<VendorDto?>;
