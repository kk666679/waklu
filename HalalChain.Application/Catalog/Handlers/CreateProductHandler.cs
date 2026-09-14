using HalalChain.Application.Common.Interfaces;
using HalalChain.Application.Catalog.Commands;
using HalalChain.Application.Common.Exceptions;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Catalog.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Application.Catalog.Handlers;

public sealed class CreateProductHandler(IProductRepository productRepository)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = await productRepository.CreateAsync(request.Request, request.VendorId, ct);
        return product.Id;
    }
}
