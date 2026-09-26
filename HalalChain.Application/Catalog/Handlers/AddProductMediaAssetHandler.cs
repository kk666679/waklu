using HalalChain.Application.Common.Interfaces;
using HalalChain.Application.Catalog.Commands;
using HalalChain.Application.Common.Exceptions;
using HalalChain.Domain.Catalog;
using MediatR;

namespace HalalChain.Application.Catalog.Handlers;

/// <summary>
/// Handler for adding media assets to products.
/// </summary>
public sealed class AddProductMediaAssetHandler(IProductRepository productRepository)
    : IRequestHandler<AddProductMediaAssetCommand, Guid>
{
    public async Task<Guid> Handle(AddProductMediaAssetCommand request, CancellationToken ct)
    {
        // Verify product exists and belongs to vendor
        var productExists = await productRepository.IsOwnedByVendorAsync(request.ProductId, request.VendorId, ct);
        if (!productExists)
        {
            throw new ValidationException("Product not found or does not belong to vendor.");
        }

        // Create the media asset
        var mediaAsset = new ProductMediaAsset
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Url = request.Url,
            Type = request.Type,
            AltText = request.AltText,
            Caption = request.Caption,
            SortOrder = request.SortOrder,
            IsPrimary = request.IsPrimary,
            BlurHash = request.BlurHash,
            VideoDurationSeconds = request.VideoDurationSeconds,
            VideoThumbnailUrl = request.VideoThumbnailUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // TODO: Persist via repository or direct EF Core context
        // For now, assuming repository pattern would handle this
        // Example: await productRepository.AddMediaAssetAsync(mediaAsset, ct);

        return mediaAsset.Id;
    }
}
