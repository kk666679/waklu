using MediatR;

namespace HalalChain.Application.Catalog.Commands;

/// <summary>
/// Command to add a media asset (image, video, etc.) to a product.
/// </summary>
public record AddProductMediaAssetCommand : IRequest<Guid>
{
    public Guid ProductId { get; set; }
    public Guid VendorId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = "Image"; // Image, Video, 360View, Demo, Instruction, ThreeDModel
    public string? AltText { get; set; }
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public string? BlurHash { get; set; }
    public int? VideoDurationSeconds { get; set; }
    public string? VideoThumbnailUrl { get; set; }
}
