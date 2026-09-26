using MediatR;

namespace HalalChain.Application.Catalog.Queries;

/// <summary>
/// Query to get personalized product recommendations.
/// Supports multiple recommendation types: personalized, trending, similar, frequently_bought_together.
/// </summary>
public record GetProductRecommendationsQuery : IRequest<List<ProductRecommendation>>
{
    /// <summary>
    /// User ID for personalized recommendations.
    /// </summary>
    public Guid? UserId { get; set; }
    
    /// <summary>
    /// Recommendation type: personalized, trending, similar, frequently_bought_together.
    /// </summary>
    public string Type { get; set; } = "personalized";
    
    /// <summary>
    /// Product ID (for similar or frequently_bought_together types).
    /// </summary>
    public Guid? ProductId { get; set; }
    
    /// <summary>
    /// Maximum number of recommendations.
    /// </summary>
    public int Limit { get; set; } = 10;
}

public record ProductRecommendation
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal AverageRating { get; set; }
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Why this product is recommended (AI-generated reason).
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence score for the recommendation (0-1).
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// Algorithm used: collaborative, content_based, trending, trending.
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;
}
