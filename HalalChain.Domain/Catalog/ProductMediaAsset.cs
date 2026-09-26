namespace HalalChain.Domain.Catalog;

/// <summary>
/// Media asset for a product (image, video, 360 view, etc.).
/// Supports multiple formats, optimization metadata, and accessibility.
/// </summary>
public sealed class ProductMediaAsset
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>
    /// Primary URL to the asset (CDN-served).
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Type of media: Image, Video, 360View, Demo, Instruction, ThreeDModel.
    /// </summary>
    public string Type { get; set; } = "Image";

    /// <summary>
    /// Alt text for accessibility (screen readers, SEO).
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Human-readable caption or label.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Sort order for display in galleries/carousels.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this is the primary/thumbnail asset for the product.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Blurhash placeholder string for blur-up loading effect.
    /// </summary>
    public string? BlurHash { get; set; }

    /// <summary>
    /// Video duration in seconds (if Type = Video).
    /// </summary>
    public int? VideoDurationSeconds { get; set; }

    /// <summary>
    /// Video thumbnail URL (auto-generated from first frame).
    /// </summary>
    public string? VideoThumbnailUrl { get; set; }

    /// <summary>
    /// Transcript of video content (for accessibility + SEO).
    /// </summary>
    public string? VideoTranscript { get; set; }

    /// <summary>
    /// Attribution or licensing information.
    /// </summary>
    public string? Attribution { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
