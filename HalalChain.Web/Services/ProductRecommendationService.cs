using Microsoft.ML;
using Microsoft.ML.Data;
using HalalChain.Platform.Contracts.Catalog.Dto;

namespace HalalChain.Services;

public class ProductInteraction
{
    [LoadColumn(0)] public float UserId { get; set; }
    [LoadColumn(1)] public float ProductId { get; set; }
    [LoadColumn(2)] public float Label { get; set; }
}

public class ProductPrediction
{
    [ColumnName("Score")] public float Score { get; set; }
}

public interface IProductRecommendationService
{
    Task<List<ProductDto>> GetRelatedProducts(Guid productId, int count = 6);
    Task<List<ProductDto>> GetPersonalizedRecommendations(string userId, int count = 8);
}

public class ProductRecommendationService : IProductRecommendationService
{
    private readonly AppState _state;
    private readonly ILogger<ProductRecommendationService> _logger;

    public ProductRecommendationService(AppState state, ILogger<ProductRecommendationService> logger)
    {
        _state = state;
        _logger = logger;
    }

    public async Task<List<ProductDto>> GetRelatedProducts(Guid productId, int count = 6)
    {
        if (!_state.Products.Any())
            await _state.LoadProductsAsync();

        var product = _state.Products.FirstOrDefault(p => p.Id == productId);
        if (product == null) return [];

        // Content-based: same category, exclude self, sort by price similarity
        var related = _state.Products
            .Where(p => p.Id != productId && p.CategoryName == product.CategoryName)
            .OrderBy(p => Math.Abs(p.Price - product.Price))
            .Take(count)
            .ToList();

        if (related.Count < count)
        {
            var extras = _state.Products
                .Where(p => p.Id != productId && !related.Any(r => r.Id == p.Id))
                .OrderBy(p => p.HalalStatus?.Status == "Verified" ? 0 : 1)
                .ThenBy(p => Math.Abs(p.Price - product.Price))
                .Take(count - related.Count);
            related.AddRange(extras);
        }

        return related;
    }

    public async Task<List<ProductDto>> GetPersonalizedRecommendations(string userId, int count = 8)
    {
        if (!_state.Products.Any())
            await _state.LoadProductsAsync();

        // Fallback: return top products by inventory and halal status
        return _state.Products
            .OrderByDescending(p => p.HalalStatus?.Status == "Verified" ? 1 : 0)
            .ThenByDescending(p => p.Inventory)
            .Take(count)
            .ToList();
    }
}
