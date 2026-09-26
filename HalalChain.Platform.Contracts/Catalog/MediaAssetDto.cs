namespace HalalChain.Platform.Contracts.Catalog;

/// <summary>
/// Represents a single media asset (image, video, 360-view, etc.) for a product.
/// Supports multiple formats, optimization metadata, and accessibility.
/// </summary>
public sealed class MediaAssetDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Primary URL to the asset (CDN-served).
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Type of media: "Image", "Video", "360View", "Demo", "Instruction", "ThreeDModel".
    /// </summary>
    public string Type { get; set; } = "Image";

    /// <summary>
    /// Alt text for accessibility (screen readers, SEO).
    /// Auto-generated if not provided.
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Human-readable caption or label.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Sort order for display in galleries/carousels.
    /// Lower numbers appear first.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this is the primary/thumbnail asset for the product.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Blurhash placeholder string for blur-up loading effect.
    /// Computed server-side during upload.
    /// </summary>
    public string? BlurHash { get; set; }

    /// <summary>
    /// CDN optimization data: available formats (WEBP, AVIF, JPEG, etc.)
    /// and sizes (thumbnail, small, medium, large, full).
    /// </summary>
    public MediaOptimizationDto? Optimization { get; set; }

    /// <summary>
    /// Video metadata if Type is "Video".
    /// </summary>
    public VideoMetadataDto? VideoMetadata { get; set; }

    /// <summary>
    /// Attribution or licensing information.
    /// </summary>
    public string? Attribution { get; set; }

    /// <summary>
    /// Creation/upload timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// CDN optimization metadata for responsive image serving.
/// </summary>
public sealed class MediaOptimizationDto
{
    /// <summary>
    /// Available image formats: "jpeg", "webp", "avif", "png".
    /// </summary>
    public List<string> AvailableFormats { get; set; } = [];

    /// <summary>
    /// Available sizes with dimensions.
    /// Example: { "thumbnail": "150x150", "small": "300x300", "large": "1200x1200" }
    /// </summary>
    public Dictionary<string, string> AvailableSizes { get; set; } = [];

    /// <summary>
    /// Recommended CDN parameters for srcset generation.
    /// </summary>
    public string? SrcSetTemplate { get; set; }

    /// <summary>
    /// Total file size in bytes for the primary format.
    /// </summary>
    public long? FileSizeBytes { get; set; }
}

/// <summary>
/// Video-specific metadata.
/// </summary>
public sealed class VideoMetadataDto
{
    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Thumbnail URL (auto-generated from video first frame if not provided).
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Video codec (e.g., "h264", "vp9", "av1").
    /// </summary>
    public string? Codec { get; set; }

    /// <summary>
    /// Resolution (e.g., "1920x1080", "4K").
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// Whether subtitles are available.
    /// </summary>
    public bool HasSubtitles { get; set; }

    /// <summary>
    /// Transcript of video content (for accessibility + SEO).
    /// </summary>
    public string? Transcript { get; set; }
}
