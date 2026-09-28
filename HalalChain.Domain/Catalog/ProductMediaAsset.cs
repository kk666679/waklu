namespace HalalChain.Domain.Catalog;

using HalalChain.Domain.Common;

/// <summary>
/// A media asset attached to a product. The blob is referenced by its
/// content hash — a string here, not a BlobRef, because BlobRef lives in
/// the Application layer's storage ports and Domain must not reference it.
/// </summary>
public sealed class ProductMediaAsset : Entity<ProductMediaAssetId>
{
    public string ContentHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "primary";
    public string ContentType { get; private set; } = "image/jpeg";
    public int SortOrder { get; private set; }

    private ProductMediaAsset() { }

    public static ProductMediaAsset Create(
        string contentHash,
        string contentType,
        string role,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        return new ProductMediaAsset
        {
            Id = ProductMediaAssetId.New(),
            ContentHash = contentHash,
            ContentType = contentType,
            Role = role,
            SortOrder = sortOrder,
        };
    }
}
