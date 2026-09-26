using HalalChain.Application.Common;
using MediatR;

namespace HalalChain.Application.Catalog.Queries;

/// <summary>
/// Query for advanced product search with facets, filters, and pagination.
/// Supports full-text search, semantic search, and AI-powered discovery.
/// </summary>
public record SearchProductsQuery : IRequest<PagedResult<ProductSearchResult>>
{
    /// <summary>
    /// Search query text.
    /// </summary>
    public string? Query { get; set; }
    
    /// <summary>
    /// Category ID filter.
    /// </summary>
    public Guid? CategoryId { get; set; }
    
    /// <summary>
    /// Price range filter (min and max).
    /// </summary>
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    
    /// <summary>
    /// Minimum rating filter (0-5).
    /// </summary>
    public decimal? MinRating { get; set; }
    
    /// <summary>
    /// Tag filters.
    /// </summary>
    public List<string> Tags { get; set; } = [];
    
    /// <summary>
    /// Attribute value filters.
    /// </summary>
    public Dictionary<string, List<string>> AttributeFilters { get; set; } = [];
    
    /// <summary>
    /// Whether to show only in-stock products.
    /// </summary>
    public bool InStockOnly { get; set; }
    
    /// <summary>
    /// Whether to filter by halal certification.
    /// </summary>
    public bool? HalalOnly { get; set; }
    
    /// <summary>
    /// Sustainability filter (min eco-rating 1-5).
    /// </summary>
    public int? MinEcoRating { get; set; }
    
    /// <summary>
    /// Sort order: relevance, price_asc, price_desc, rating, newest, popularity.
    /// </summary>
    public string SortBy { get; set; } = "relevance";
    
    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Items per page.
    /// </summary>
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// Enable semantic search (vector similarity).
    /// </summary>
    public bool UseSemanticSearch { get; set; }
}

public record ProductSearchResult
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsHalal { get; set; }
    public bool IsOutOfStock { get; set; }
    public string? ImageUrl { get; set; }
    public string? Brand { get; set; }
    public List<string> Tags { get; set; } = [];
    public int? EcoRating { get; set; }
    public string? RecommendationReason { get; set; }
    public float? SemanticSimilarityScore { get; set; }
}
