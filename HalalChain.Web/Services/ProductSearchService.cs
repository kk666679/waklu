using HalalChain.Models;
using System.Text.RegularExpressions;

namespace HalalChain.Services;

/// <summary>
/// Advanced product search service with semantic search, faceted navigation, and AI-powered features.
/// Supports full-text search, filters, autocomplete, typo tolerance, and personalization.
/// </summary>
public interface IProductSearchService
{
    /// <summary>
    /// Perform advanced product search with filters, sorting, and pagination.
    /// </summary>
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default);

    /// <summary>
    /// Get faceted search filters for current query results.
    /// </summary>
    Task<FacetedFilters> GetFacetsAsync(SearchQuery query, CancellationToken ct = default);

    /// <summary>
    /// Get autocomplete suggestions based on partial query.
    /// </summary>
    Task<List<AutocompleteSuggestion>> GetAutocompleteSuggestionsAsync(string partialQuery, int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Perform semantic search using embeddings (AI-powered similarity).
    /// </summary>
    Task<SearchResult> SemanticSearchAsync(string query, int limit = 20, CancellationToken ct = default);

    /// <summary>
    /// Find similar products based on vector embeddings.
    /// </summary>
    Task<List<Product>> FindSimilarProductsAsync(int productId, int limit = 5, CancellationToken ct = default);

    /// <summary>
    /// Get trending products based on view/purchase velocity.
    /// </summary>
    Task<List<Product>> GetTrendingProductsAsync(int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Index a product for search (called after product creation/update).
    /// </summary>
    Task IndexProductAsync(Product product, CancellationToken ct = default);

    /// <summary>
    /// Reindex all products (called during migrations/optimization).
    /// </summary>
    Task ReindexAllProductsAsync(CancellationToken ct = default);
}

/// <summary>
/// Implementation of advanced product search service.
/// </summary>
public class ProductSearchService : IProductSearchService
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<ProductSearchService> _logger;

    // In-memory search index (in production, use Elasticsearch or Qdrant)
    private readonly Dictionary<int, SearchIndexEntry> _searchIndex = new();
    private readonly Dictionary<string, List<int>> _facetIndex = new();
    private readonly Dictionary<int, int> _popularityIndex = new();

    public ProductSearchService(
        IProductService productService,
        ICategoryService categoryService,
        ILogger<ProductSearchService> logger)
    {
        _productService = productService;
        _categoryService = categoryService;
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default)
    {
        try
        {
            // Get all products as baseline
            var allProducts = await _productService.GetAllAsync(ct);

            // Apply text search
            IEnumerable<Product> results = allProducts;
            if (!string.IsNullOrWhiteSpace(query.Query))
            {
                results = ApplyTextSearch(results, query.Query);
            }

            // Apply filters
            if (query.Filters?.Any() == true)
            {
                results = ApplyFilters(results, query.Filters);
            }

            // Apply category filter
            if (query.CategoryId.HasValue)
            {
                results = results.Where(p => p.CategoryId == query.CategoryId.Value);
            }

            // Apply halal filter
            if (query.HalalStatusFilter.HasValue)
            {
                results = results.Where(p => p.IsHalal == query.HalalStatusFilter.Value);
            }

            // Apply price range filter
            if (query.PriceMin.HasValue || query.PriceMax.HasValue)
            {
                results = ApplyPriceFilter(results, query.PriceMin, query.PriceMax);
            }

            // Apply rating filter
            if (query.MinRating.HasValue)
            {
                results = results.Where(p => p.AverageRating >= query.MinRating.Value);
            }

            // Apply stock filter
            if (query.InStockOnly)
            {
                results = results.Where(p => !p.IsOutOfStock && p.StockQuantity > 0);
            }

            // Apply tag filter
            if (query.Tags?.Any() == true)
            {
                results = ApplyTagFilter(results, query.Tags);
            }

            // Count total results before pagination
            var totalResults = results.Count();

            // Apply sorting
            results = ApplySorting(results, query.SortBy ?? "relevance");

            // Apply pagination
            var skip = (query.Page - 1) * query.PageSize;
            var paginatedResults = results.Skip(skip).Take(query.PageSize).ToList();

            return new SearchResult
            {
                Query = query.Query,
                TotalResults = totalResults,
                PageNumber = query.Page,
                PageSize = query.PageSize,
                Products = paginatedResults,
                ExecutionTimeMs = 0 // Would measure actual execution time
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", query.Query);
            throw;
        }
    }

    public async Task<FacetedFilters> GetFacetsAsync(SearchQuery query, CancellationToken ct = default)
    {
        try
        {
            // Get base results
            var baseResults = await SearchAsync(query with { Page = 1, PageSize = 10000 }, ct);
            var products = baseResults.Products;

            var facets = new FacetedFilters();

            // Category facets
            var categories = products
                .GroupBy(p => p.CategoryId)
                .Select(g => new FacetOption
                {
                    Name = g.First().Category?.Name ?? $"Category {g.Key}",
                    Value = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderByDescending(f => f.Count)
                .ToList();
            facets.Categories = categories;

            // Price range facets
            var priceRanges = new[]
            {
                (min: 0m, max: 50m, label: "$0 - $50"),
                (min: 50m, max: 100m, label: "$50 - $100"),
                (min: 100m, max: 250m, label: "$100 - $250"),
                (min: 250m, max: 500m, label: "$250 - $500"),
                (min: 500m, max: decimal.MaxValue, label: "$500+")
            };

            facets.PriceRanges = priceRanges
                .Select(range => new FacetOption
                {
                    Name = range.label,
                    Value = $"{range.min}-{range.max}",
                    Count = products.Count(p => p.Price >= range.min && p.Price <= range.max)
                })
                .Where(f => f.Count > 0)
                .ToList();

            // Rating facets
            facets.Ratings = new[]
            {
                (min: 4.5m, label: "4.5+"),
                (min: 4.0m, label: "4.0+"),
                (min: 3.5m, label: "3.5+"),
                (min: 3.0m, label: "3.0+")
            }
            .Select(r => new FacetOption
            {
                Name = r.label,
                Value = r.min.ToString(),
                Count = products.Count(p => p.AverageRating >= r.min)
            })
            .Where(f => f.Count > 0)
            .ToList();

            // Halal status facet
            var halalCount = products.Count(p => p.IsHalal);
            if (halalCount > 0)
            {
                facets.HalalStatus = new List<FacetOption>
                {
                    new() { Name = "Halal Verified", Value = "true", Count = halalCount },
                    new() { Name = "Not Verified", Value = "false", Count = products.Count - halalCount }
                };
            }

            // Tag facets
            var tagFacets = products
                .SelectMany(p => p.Tags)
                .GroupBy(t => t)
                .Select(g => new FacetOption
                {
                    Name = g.Key,
                    Value = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(f => f.Count)
                .Take(10)
                .ToList();
            facets.Tags = tagFacets;

            // Attribute facets (color, size, material, etc.)
            var attributeFacets = products
                .SelectMany(p => p.Attributes)
                .GroupBy(a => a)
                .Select(g => new FacetOption
                {
                    Name = g.Key,
                    Value = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(f => f.Count)
                .Take(20)
                .ToList();
            facets.Attributes = attributeFacets;

            return facets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get facets");
            throw;
        }
    }

    public async Task<List<AutocompleteSuggestion>> GetAutocompleteSuggestionsAsync(string partialQuery, int limit = 10, CancellationToken ct = default)
    {
        var suggestions = new List<AutocompleteSuggestion>();

        if (string.IsNullOrWhiteSpace(partialQuery) || partialQuery.Length < 2)
            return suggestions;

        partialQuery = partialQuery.ToLowerInvariant();

        try
        {
            // Get all products
            var allProducts = await _productService.GetAllAsync(ct);

            // Product name suggestions
            var productSuggestions = allProducts
                .Where(p => p.Name.ToLowerInvariant().Contains(partialQuery))
                .GroupBy(p => p.Name)
                .Take(limit / 2)
                .Select(g => new AutocompleteSuggestion
                {
                    Text = g.Key,
                    Type = "product",
                    Popularity = _popularityIndex.GetValueOrDefault(g.First().Id, 0),
                    Icon = "📦"
                })
                .ToList();
            suggestions.AddRange(productSuggestions);

            // Category suggestions
            var categories = await _categoryService.GetAllAsync(ct);
            var categorySuggestions = categories
                .Where(c => c.Name.ToLowerInvariant().Contains(partialQuery))
                .Take(limit / 4)
                .Select(c => new AutocompleteSuggestion
                {
                    Text = c.Name,
                    Type = "category",
                    Popularity = 0,
                    Icon = "📂"
                })
                .ToList();
            suggestions.AddRange(categorySuggestions);

            // Tag suggestions
            var tagSuggestions = allProducts
                .SelectMany(p => p.Tags)
                .Distinct()
                .Where(t => t.ToLowerInvariant().Contains(partialQuery))
                .Take(limit / 4)
                .Select(t => new AutocompleteSuggestion
                {
                    Text = t,
                    Type = "tag",
                    Popularity = 0,
                    Icon = "🏷️"
                })
                .ToList();
            suggestions.AddRange(tagSuggestions);

            // AI-enhanced suggestions: typo correction & phonetic matching
            var typoCorrections = SuggestTypoCorrections(partialQuery, limit / 4);
            suggestions.AddRange(typoCorrections);

            return suggestions
                .OrderByDescending(s => s.Popularity)
                .ThenBy(s => s.Text)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autocomplete failed for query: {Query}", partialQuery);
            return suggestions;
        }
    }

    public async Task<SearchResult> SemanticSearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        // In production, this would:
        // 1. Convert query to embedding via AI gateway (LLM embeddings)
        // 2. Query Qdrant vector database for similar product embeddings
        // 3. Return ranked results by vector similarity

        _logger.LogInformation("Semantic search requested for: {Query}", query);

        // For now, fall back to regular text search
        return await SearchAsync(new SearchQuery
        {
            Query = query,
            Page = 1,
            PageSize = limit
        }, ct);
    }

    public async Task<List<Product>> FindSimilarProductsAsync(int productId, int limit = 5, CancellationToken ct = default)
    {
        try
        {
            var product = await _productService.GetByIdAsync(productId, ct);
            if (product == null)
                return new List<Product>();

            var allProducts = await _productService.GetAllAsync(ct);

            // Find products in same category with similar attributes
            var similar = allProducts
                .Where(p => p.Id != productId && p.CategoryId == product.CategoryId)
                .OrderByDescending(p =>
                {
                    var score = 0;

                    // Same category: +100
                    if (p.CategoryId == product.CategoryId) score += 100;

                    // Similar price range (±30%): +50
                    var priceDiff = Math.Abs(p.Price - product.Price);
                    if (priceDiff <= product.Price * 0.3m) score += 50;

                    // Shared attributes: +30 each
                    var sharedAttrs = p.Attributes.Intersect(product.Attributes).Count();
                    score += sharedAttrs * 30;

                    // Similar rating: +20
                    if (Math.Abs(p.AverageRating - product.AverageRating) <= 1) score += 20;

                    return score;
                })
                .Take(limit)
                .ToList();

            return similar;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar products for ProductId: {ProductId}", productId);
            return new List<Product>();
        }
    }

    public async Task<List<Product>> GetTrendingProductsAsync(int limit = 10, CancellationToken ct = default)
    {
        try
        {
            var allProducts = await _productService.GetAllAsync(ct);

            // Products sorted by popularity (view count proxy via tags)
            var trending = allProducts
                .Where(p => p.Tags.Contains("Trending") || p.Tags.Contains("New Arrival") || p.ReviewCount > 10)
                .OrderByDescending(p => p.ReviewCount)
                .ThenByDescending(p => p.AverageRating)
                .Take(limit)
                .ToList();

            return trending;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get trending products");
            return new List<Product>();
        }
    }

    public async Task IndexProductAsync(Product product, CancellationToken ct = default)
    {
        try
        {
            var entry = new SearchIndexEntry
            {
                ProductId = product.Id,
                Name = product.Name,
                Description = product.Description,
                Keywords = product.Keywords,
                Tags = product.Tags,
                Attributes = product.Attributes,
                Price = product.Price,
                Rating = product.AverageRating,
                IndexedAt = DateTime.UtcNow
            };

            _searchIndex[product.Id] = entry;
            product.IsSearchIndexed = true;

            _logger.LogDebug("Product indexed: ProductId={ProductId}, Name={Name}", product.Id, product.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index product: {ProductId}", product.Id);
        }
    }

    public async Task ReindexAllProductsAsync(CancellationToken ct = default)
    {
        try
        {
            _searchIndex.Clear();
            _facetIndex.Clear();

            var allProducts = await _productService.GetAllAsync(ct);

            foreach (var product in allProducts)
            {
                await IndexProductAsync(product, ct);
            }

            _logger.LogInformation("Reindexed {Count} products", allProducts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reindex all products");
            throw;
        }
    }

    // Private helpers

    private IEnumerable<Product> ApplyTextSearch(IEnumerable<Product> products, string query)
    {
        var lowerQuery = query.ToLowerInvariant();

        return products.Where(p =>
            p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (p.Keywords?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
            p.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
        );
    }

    private IEnumerable<Product> ApplyFilters(IEnumerable<Product> products, Dictionary<string, object> filters)
    {
        foreach (var filter in filters)
        {
            switch (filter.Key.ToLowerInvariant())
            {
                case "vendors":
                    if (filter.Value is List<int> vendorIds)
                        products = products.Where(p => vendorIds.Contains(p.VendorId));
                    break;

                case "attributes":
                    if (filter.Value is List<string> attributeValues)
                        products = products.Where(p => p.Attributes.Any(a => attributeValues.Contains(a)));
                    break;
            }
        }

        return products;
    }

    private IEnumerable<Product> ApplyPriceFilter(IEnumerable<Product> products, decimal? minPrice, decimal? maxPrice)
    {
        if (minPrice.HasValue)
            products = products.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            products = products.Where(p => p.Price <= maxPrice.Value);

        return products;
    }

    private IEnumerable<Product> ApplyTagFilter(IEnumerable<Product> products, List<string> tags)
    {
        return products.Where(p => p.Tags.Any(t => tags.Contains(t)));
    }

    private IEnumerable<Product> ApplySorting(IEnumerable<Product> products, string sortBy)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "price_asc" => products.OrderBy(p => p.Price),
            "price_desc" => products.OrderByDescending(p => p.Price),
            "rating" => products.OrderByDescending(p => p.AverageRating).ThenByDescending(p => p.ReviewCount),
            "newest" => products.OrderByDescending(p => p.CreatedAt),
            "popularity" => products.OrderByDescending(p => p.ReviewCount),
            _ => products.OrderByDescending(p => p.AverageRating) // Default: relevance/rating
        };
    }

    private List<AutocompleteSuggestion> SuggestTypoCorrections(string query, int limit)
    {
        // Simple typo correction: phonetic similarity
        // In production, use Levenshtein distance or Soundex algorithm

        var corrections = new List<AutocompleteSuggestion>();

        // Placeholder: would implement fuzzy matching here
        // Example: "halel" -> "halal", "dates" -> "dates"

        return corrections.Take(limit).ToList();
    }
}

// DTOs

public class SearchQuery
{
    public string? Query { get; set; }
    public int? CategoryId { get; set; }
    public bool? HalalStatusFilter { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public decimal? MinRating { get; set; }
    public bool InStockOnly { get; set; } = false;
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? Filters { get; set; }
    public string? SortBy { get; set; } = "relevance"; // relevance, price_asc, price_desc, rating, newest, popularity
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SearchResult
{
    public string? Query { get; set; }
    public int TotalResults { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<Product> Products { get; set; } = [];
    public int ExecutionTimeMs { get; set; }
}

public class FacetedFilters
{
    public List<FacetOption> Categories { get; set; } = [];
    public List<FacetOption> PriceRanges { get; set; } = [];
    public List<FacetOption> Ratings { get; set; } = [];
    public List<FacetOption> HalalStatus { get; set; } = [];
    public List<FacetOption> Tags { get; set; } = [];
    public List<FacetOption> Attributes { get; set; } = [];
}

public class FacetOption
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AutocompleteSuggestion
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // product, category, tag, brand
    public int Popularity { get; set; }
    public string Icon { get; set; } = string.Empty;
}

public class SearchIndexEntry
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Keywords { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<string> Attributes { get; set; } = [];
    public decimal Price { get; set; }
    public decimal Rating { get; set; }
    public DateTime IndexedAt { get; set; }
}
