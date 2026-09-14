using System.ComponentModel.DataAnnotations;

namespace HalalChain.Platform.Contracts.Catalog.Requests;

public sealed class CreateProductRequest
{
    [Required, MinLength(1), MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Required]
    public Guid CategoryId { get; init; }

    [Required, Range(0.01, 999999.99)]
    public decimal Price { get; init; }

    [Required, MinLength(1), MaxLength(3)]
    public string Currency { get; init; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Inventory { get; init; }

    [Required, MinLength(1), MaxLength(100)]
    public string Origin { get; init; } = string.Empty;
}

public sealed class UpdateProductRequest
{
    [MinLength(1), MaxLength(200)]
    public string? Title { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Range(0.01, 999999.99)]
    public decimal? Price { get; init; }

    [Range(0, int.MaxValue)]
    public int? Inventory { get; init; }
}
