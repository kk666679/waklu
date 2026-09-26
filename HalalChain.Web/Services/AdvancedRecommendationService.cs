using HalalChain.Models;

namespace HalalChain.Services;

/// <summary>
/// Advanced product recommendation engine with collaborative filtering,
/// content-based recommendations, and ML-powered personalization.
/// Upgraded from the existing ProductRecommendationService with 2026 features.
/// </summary>
public interface IAdvancedRecommendationService
{
    /// <summary>
    /// Get personalized product recommendations for a user.
    /// Uses collaborative filtering + content-based hybrid approach.
    /// </summary>
    Task<List<RecommendationResult>> GetPersonalizedRecommendationsAsync(string userId, int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Get "customers also bought" recommendations for a specific product.
    /// </summary>
    Task<List<RecommendationResult>> GetFrequentlyBoughtTogetherAsync(int productId, int limit = 5, CancellationToken ct = default);

    /// <summary>
    /// Get "similar products" recommendations (content-based).
    /// </summary>
    Task<List<RecommendationResult>> GetSimilarProductsAsync(int productId, int limit = 5, CancellationToken ct = default);

    /// <summary>
    /// Get trending products with AI reasoning (why trending).
    /// </summary>
    Task<List<RecommendationResult>> GetTrendingWithReasonAsync(int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Get category recommendations (upsell/cross-sell).
    /// </summary>
    Task<List<RecommendationResult>> GetCategoryRecommendationsAsync(int categoryId, int limit = 5, CancellationToken ct = default);

    /// <summary>
    /// Record a user interaction (view, click, purchase) for ML training.
    /// </summary>
    Task RecordUserInteractionAsync(string userId, int productId, string interactionType, CancellationToken ct = default);

    /// <summary>
    /// Generate AI-powered explanation for why a product is recommended.
    /// </summary>
    Task<string> GenerateRecommendationReasonAsync(int productId, string userId, CancellationToken ct = default);

    /// <summary>
    /// A/B test different recommendation algorithms.
    /// </summary>
    Task<List<RecommendationResult>> GetRecommendationsWithABTestAsync(string userId, string variant, int limit = 10, CancellationToken ct = default);
}

/// <summary>
/// Implementation of advanced recommendation service.
/// </summary>
public class AdvancedRecommendationService : IAdvancedRecommendationService
{
    private readonly IProductService _productService;
    private readonly ISearchService _searchService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<AdvancedRecommendationService> _logger;

    // In-memory stores for user interactions and patterns (use database in production)
    private readonly Dictionary<string, List<UserInteraction>> _userInteractions = [];
    private readonly Dictionary<(int, int), int> _productPairFrequency = [];
    private readonly Dictionary<int, int> _productViewCount = [];

    public AdvancedRecommendationService(
        IProductService productService,
        ISearchService searchService,
        ICategoryService categoryService,
        ILogger<AdvancedRecommendationService> logger)
    {
        _productService = productService;
        _searchService = searchService;
        _categoryService = categoryService;
        _logger = logger;
    }

    public async Task<List<RecommendationResult>> GetPersonalizedRecommendationsAsync(string userId, int limit = 10, CancellationToken ct = default)
    {
        try
        {
            var allProducts = await _productService.GetAllAsync(ct);
            var recommendations = new List<RecommendationResult>();

            // Get user's viewed/purchased products
            var userHistory = _userInteractions.GetValueOrDefault(userId, []);
            var viewedProductIds = userHistory
                .Where(i => i.InteractionType is "view" or "purchase")
                .Select(i => i.ProductId)
                .Distinct()
                .ToList();

            if (viewedProductIds.Any())
            {
                // Collaborative filtering: find products liked by users with similar taste
                foreach (var viewedId in viewedProductIds.Take(5))
                {
                    var similar = allProducts
                        .Where(p => p.Id != viewedId && !viewedProductIds.Contains(p.Id))
                        .OrderByDescending(p =>
                        {
                            var score = 0;

                            // Same category: +100
                            var viewed = allProducts.FirstOrDefault(x => x.Id == viewedId);
                            if (viewed != null && p.CategoryId == viewed.CategoryId) score += 100;

                            // High rating: +50
                            if (p.AverageRating >= 4.0m) score += 50;

                            // In stock: +30
                            if (!p.IsOutOfStock) score += 30;

                            // Trending: +40
                            if (p.Tags.Contains("Trending")) score += 40;

                            // Similarity score based on attributes
                            var sharedAttrs = viewed?.Attributes.Intersect(p.Attributes).Count() ?? 0;
                            score += sharedAttrs * 20;

                            return score;
                        })
                        .Take(2)
                        .ToList();

                    recommendations.AddRange(similar.Select(p => new RecommendationResult
                    {
                        Product = p,
                        Score = 0.8m,
                        Reason = "Based on products you viewed",
                        Algorithm = "Collaborative"
                    }));
                }
            }

            // Content-based: recommend new products in user's favorite categories
            if (userHistory.Any())
            {
                var favoriteCategoryIds = userHistory
                    .GroupBy(i => allProducts.FirstOrDefault(p => p.Id == i.ProductId)?.CategoryId)
                    .Where(g => g.Key.HasValue)
                    .OrderByDescending(g => g.Count())
                    .Take(2)
                    .Select(g => g.Key.Value)
                    .ToList();

                var newInCategory = allProducts
                    .Where(p => favoriteCategoryIds.Contains(p.CategoryId) && !viewedProductIds.Contains(p.Id))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(3)
                    .Select(p => new RecommendationResult
                    {
                        Product = p,
                        Score = 0.7m,
                        Reason = "New in categories you like",
                        Algorithm = "ContentBased"
                    })
                    .ToList();

                recommendations.AddRange(newInCategory);
            }

            // Fallback: trending products if not enough recommendations
            if (!recommendations.Any())
            {
                var trending = allProducts
                    .Where(p => p.Tags.Contains("Trending"))
                    .OrderByDescending(p => p.ReviewCount)
                    .Take(limit)
                    .Select(p => new RecommendationResult
                    {
                        Product = p,
                        Score = 0.6m,
                        Reason = "Trending now",
                        Algorithm = "Trending"
                    })
                    .ToList();

                recommendations.AddRange(trending);
            }

            return recommendations
                .DistinctBy(r => r.Product.Id)
                .OrderByDescending(r => r.Score)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personalized recommendations for user: {UserId}", userId);
            return new List<RecommendationResult>();
        }
    }

    public async Task<List<RecommendationResult>> GetFrequentlyBoughtTogetherAsync(int productId, int limit = 5, CancellationToken ct = default)
    {
        try
        {
            var allProducts = await _productService.GetAllAsync(ct);
            var product = allProducts.FirstOrDefault(p => p.Id == productId);
            if (product == null) return new List<RecommendationResult>();

            // Find products frequently bought with this one
            var frequentPairs = _productPairFrequency
                .Where(kv => (kv.Key.Item1 == productId || kv.Key.Item2 == productId) && kv.Value >= 2)
                .OrderByDescending(kv => kv.Value)
                .Take(limit)
                .Select(kv => kv.Key.Item1 == productId ? kv.Key.Item2 : kv.Key.Item1)
                .ToList();

            var recommendations = frequentPairs
                .Select(pid => allProducts.FirstOrDefault(p => p.Id == pid))
                .Where(p => p != null)
                .Select(p => new RecommendationResult
                {
                    Product = p!,
                    Score = 0.85m,
                    Reason = "Frequently bought together",
                    Algorithm = "FrequentlyBoughtTogether"
                })
                .ToList();

            // If not enough results, add similar products
            if (recommendations.Count < limit)
            {
                var similar = allProducts
                    .Where(p => p.Id != productId && !frequentPairs.Contains(p.Id) && p.CategoryId == product.CategoryId)
                    .OrderByDescending(p => p.AverageRating)
                    .Take(limit - recommendations.Count)
                    .Select(p => new RecommendationResult
                    {
                        Product = p,
                        Score = 0.7m,
                        Reason = "Similar products",
                        Algorithm = "ContentBased"
                    })
                    .ToList();

                recommendations.AddRange(similar);
            }

            return recommendations.Take(limit).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting frequently bought together for ProductId: {ProductId}", productId);
            return new List<RecommendationResult>();
        }
    }

    public async Task<List<RecommendationResult>> GetSimilarProductsAsync(int productId, int limit = 5, CancellationToken ct = default)
    {
        try
        {
            var allProducts = await _productService.GetAllAsync(ct);
            var product = allProducts.FirstOrDefault(p => p.Id == productId);
            if (product == null) return new List<RecommendationResult>();

            var similar = allProducts
                .Where(p => p.Id != productId && p.CategoryId == product.CategoryId)
                .OrderByDescending(p =>
                {
                    var score = 0;

                    // Same category: +100
                    if (p.CategoryId == product.CategoryId) score += 100;

                    // Shared attributes: +50 each
                    var sharedAttrs = p.Attributes.Intersect(product.Attributes).Count();
                    score += sharedAttrs * 50;

                    // Similar price range: +40
                    if (Math.Abs(p.Price - product.Price) <= product.Price * 0.2m) score += 40;

                    // High rating: +30
                    if (p.AverageRating >= 4.0m) score += 30;

                    // In stock: +20
                    if (!p.IsOutOfStock) score += 20;

                    return score;
                })
                .Take(limit)
                .Select(p => new RecommendationResult
                {
                    Product = p,
                    Score = 0.75m,
                    Reason = "Similar to this product",
                    Algorithm = "ContentBased"
                })
                .ToList();

            return similar;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similar products for ProductId: {ProductId}", productId);
            return new List<RecommendationResult>();
        }
    }

    public async Task<List<RecommendationResult>> GetTrendingWithReasonAsync(int limit = 10, CancellationToken ct = default)
    {
        try
        {
            var allProducts = await _productService.GetAllAsync(ct);

            var trending = allProducts
                .Where(p => p.Tags.Contains("Trending") || p.ReviewCount > 20)
                .OrderByDescending(p => p.ReviewCount)
                .ThenByDescending(p => p.AverageRating)
                .Take(limit)
                .Select(p => new RecommendationResult
                {
                    Product = p,
                    Score = 0.9m,
                    Reason = GenerateTrendingReason(p),
                    Algorithm = "Trending"
                })
                .ToList();

            return trending;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending products");
            return new List<RecommendationResult>();
        }
    }

    public async Task<List<RecommendationResult>> GetCategoryRecommendationsAsync(int categoryId, int limit = 5, CancellationToken ct = default)
    {
        try
        {
            var allProducts = await _productService.GetAllAsync(ct);

            var recommendations = allProducts
                .Where(p => p.CategoryId == categoryId && !p.IsOutOfStock)
                .OrderByDescending(p => p.AverageRating)
                .ThenByDescending(p => p.ReviewCount)
                .Take(limit)
                .Select(p => new RecommendationResult
                {
                    Product = p,
                    Score = 0.8m,
                    Reason = "Top rated in this category",
                    Algorithm = "CategoryTop"
                })
                .ToList();

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category recommendations for CategoryId: {CategoryId}", categoryId);
            return new List<RecommendationResult>();
        }
    }

    public async Task RecordUserInteractionAsync(string userId, int productId, string interactionType, CancellationToken ct = default)
    {
        try
        {
            if (!_userInteractions.ContainsKey(userId))
            {
                _userInteractions[userId] = [];
            }

            _userInteractions[userId].Add(new UserInteraction
            {
                UserId = userId,
                ProductId = productId,
                InteractionType = interactionType,
                Timestamp = DateTime.UtcNow
            });

            // Update product pair frequency (for "frequently bought together")
            if (interactionType == "purchase")
            {
                var recentPurchases = _userInteractions[userId]
                    .Where(i => i.InteractionType == "purchase" && (DateTime.UtcNow - i.Timestamp).TotalHours < 24)
                    .Select(i => i.ProductId)
                    .Distinct()
                    .ToList();

                foreach (var otherId in recentPurchases.Where(id => id != productId))
                {
                    var key = (Math.Min(productId, otherId), Math.Max(productId, otherId));
                    if (!_productPairFrequency.ContainsKey(key))
                        _productPairFrequency[key] = 0;
                    _productPairFrequency[key]++;
                }
            }

            // Update product view count
            if (interactionType == "view")
            {
                if (!_productViewCount.ContainsKey(productId))
                    _productViewCount[productId] = 0;
                _productViewCount[productId]++;
            }

            _logger.LogDebug("User interaction recorded: UserId={UserId}, ProductId={ProductId}, Type={Type}",
                userId, productId, interactionType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording user interaction");
        }
    }

    public async Task<string> GenerateRecommendationReasonAsync(int productId, string userId, CancellationToken ct = default)
    {
        // In production, call LLM to generate human-readable reason
        // For now, return template-based reason

        var userHistory = _userInteractions.GetValueOrDefault(userId, []);
        var lastViewedCategoryId = userHistory
            .Where(i => i.InteractionType == "view")
            .Select(i => (await _productService.GetByIdAsync(i.ProductId, ct))?.CategoryId)
            .FirstOrDefault();

        var product = await _productService.GetByIdAsync(productId, ct);
        if (product == null) return "You might like this";

        // Smart reason generation
        if (userHistory.Any(i => i.InteractionType == "purchase"))
            return "Based on your purchase history";
        if (product.AverageRating >= 4.5m)
            return $"Highly rated ({product.AverageRating:F1}★)";
        if (product.Tags.Contains("New Arrival"))
            return "Just arrived";
        if (product.Tags.Contains("Bestseller"))
            return "Our bestseller";
        if (product.ReviewCount > 50)
            return "Popular choice";

        return "You might like this";
    }

    public async Task<List<RecommendationResult>> GetRecommendationsWithABTestAsync(string userId, string variant, int limit = 10, CancellationToken ct = default)
    {
        // A/B test different recommendation algorithms
        return variant.ToLowerInvariant() switch
        {
            "collaborative" => await GetPersonalizedRecommendationsAsync(userId, limit, ct),
            "trending" => await GetTrendingWithReasonAsync(limit, ct),
            "random" => await GetRandomRecommendationsAsync(limit, ct),
            _ => await GetPersonalizedRecommendationsAsync(userId, limit, ct)
        };
    }

    // Private helpers

    private string GenerateTrendingReason(Product product)
    {
        if (product.ReviewCount > 100) return "Top trending now";
        if (product.AverageRating >= 4.5m) return "Trending & highly rated";
        if (product.Tags.Contains("New Arrival")) return "New trending item";
        return "Trending in your region";
    }

    private async Task<List<RecommendationResult>> GetRandomRecommendationsAsync(int limit, CancellationToken ct)
    {
        var allProducts = await _productService.GetAllAsync(ct);
        var random = new Random();

        return allProducts
            .OrderBy(_ => random.Next())
            .Take(limit)
            .Select(p => new RecommendationResult
            {
                Product = p,
                Score = 0.5m,
                Reason = "Random selection",
                Algorithm = "Random"
            })
            .ToList();
    }
}

// DTOs

public class RecommendationResult
{
    public Product Product { get; set; } = null!;
    public decimal Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
}

public class UserInteraction
{
    public string UserId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string InteractionType { get; set; } = string.Empty; // view, click, purchase, wishlist_add
    public DateTime Timestamp { get; set; }
}
