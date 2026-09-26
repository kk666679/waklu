using HalalChain.Application.Common.Interfaces;
using HalalChain.Application.Catalog.Commands;
using HalalChain.Application.Common.Exceptions;
using HalalChain.Domain.Catalog;
using MediatR;

namespace HalalChain.Application.Catalog.Handlers;

/// <summary>
/// Handler for updating product sustainability metrics.
/// </summary>
public sealed class UpdateProductSustainabilityHandler(IProductRepository productRepository)
    : IRequestHandler<UpdateProductSustainabilityCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProductSustainabilityCommand request, CancellationToken ct)
    {
        // Verify product exists and belongs to vendor
        var productExists = await productRepository.IsOwnedByVendorAsync(request.ProductId, request.VendorId, ct);
        if (!productExists)
        {
            throw new ValidationException("Product not found or does not belong to vendor.");
        }

        // Validate eco rating
        if (request.EcoRating.HasValue && (request.EcoRating < 1 || request.EcoRating > 5))
        {
            throw new ValidationException("Eco rating must be between 1 and 5.");
        }

        // Validate recyclable percentage
        if (request.RecyclablePercentage.HasValue && (request.RecyclablePercentage < 0 || request.RecyclablePercentage > 100))
        {
            throw new ValidationException("Recyclable percentage must be between 0 and 100.");
        }

        // Create or update sustainability record
        var sustainability = new ProductSustainability
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            CarbonFootprintKgCo2 = request.CarbonFootprintKgCo2,
            EcoRating = request.EcoRating,
            Certifications = request.Certifications,
            PackagingMaterial = request.PackagingMaterial,
            RecyclablePercentage = request.RecyclablePercentage,
            ProductionEnergyKwh = request.ProductionEnergyKwh,
            ProductionWaterLiters = request.ProductionWaterLiters,
            OriginCountry = request.OriginCountry,
            HasIso14001Certification = request.HasIso14001Certification,
            IsFairTrade = request.IsFairTrade,
            IsVegan = request.IsVegan,
            SustainabilityReportUrl = request.SustainabilityReportUrl,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // TODO: Persist via repository or direct EF Core context
        // await productRepository.UpdateSustainabilityAsync(sustainability, ct);

        return Unit.Value;
    }
}
