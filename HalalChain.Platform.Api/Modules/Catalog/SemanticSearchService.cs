using HalalChain.Platform.Api.AI;
using HalalChain.Domain.Catalog;
using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Contracts.AI.Requests;
using HalalChain.Platform.Contracts.Catalog.Dto;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Catalog;

/// <summary>
/// Generates embeddings from explicitly indexed catalog rows and ranks their
/// persisted vectors without provider-specific database operators.
/// </summary>
public sealed class SemanticSearchService
{
    private readonly IAiInferenceProvider _aiInference;
    private readonly HalalChainDbContext _db;
    private readonly ILogger<SemanticSearchService> _logger;

    public SemanticSearchService(
        IAiInferenceProvider aiInference,
        HalalChainDbContext db,
        ILogger<SemanticSearchService> logger)
    {
        _aiInference = aiInference;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Embed a real product only after it has explicitly opted into indexing.
    /// </summary>
    public async Task<ProductEmbedding?> EmbedProductAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsSearchIndexed, ct);

        return product is null ? null : await EmbedProductAsync(product, ct);
    }

    /// <summary>
    /// Search only persisted embeddings belonging to explicitly indexed products.
    /// </summary>
    public async Task<List<SemanticSearchResult>> SearchAsync(string query, int topK = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var stored = await _db.ProductEmbeddings
            .AsNoTracking()
            .Join(
                _db.Products.AsNoTracking().Where(p => p.IsSearchIndexed),
                embedding => embedding.ProductId,
                product => product.Id,
                (embedding, _) => embedding)
            .ToListAsync(ct);

        if (stored.Count == 0) return [];

        var response = await _aiInference.EmbedAsync(new EmbeddingsRequest(query), ct);
        var queryVector = response.Embedding.Select(value => (float)value).ToArray();
        if (queryVector.Length == 0) return [];

        var ranked = stored
            .Where(item => item.Dimension == queryVector.Length && item.Embedding.Length == queryVector.Length)
            .Select(item => new
            {
                item.ProductId,
                Similarity = CosineSimilarity(item.Embedding, queryVector)
            })
            .Where(item => double.IsFinite(item.Similarity))
            .OrderByDescending(item => item.Similarity)
            .Take(Math.Clamp(topK, 1, 100))
            .ToArray();

        if (ranked.Length == 0) return [];

        var productIds = ranked.Select(item => item.ProductId).ToArray();
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .Where(p => p.IsSearchIndexed && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        return ranked
            .Where(item => products.ContainsKey(item.ProductId))
            .Select(item =>
            {
                var product = products[item.ProductId];
                return new SemanticSearchResult(
                    product.Id,
                    product.Title,
                    product.Slug,
                    product.Description,
                    product.Vendor?.Name ?? string.Empty,
                    product.Category?.Name ?? string.Empty,
                    product.Price,
                    product.Currency,
                    product.Origin,
                    item.Similarity);
            })
            .ToList();
    }

    /// <summary>
    /// Generate embeddings for all products in the catalog.
    /// </summary>
    public async Task<int> EmbedAllProductsAsync(CancellationToken ct = default)
    {
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsSearchIndexed)
            .ToListAsync(ct);
        var embedded = 0;

        foreach (var product in products)
        {
            var emb = await EmbedProductAsync(product, ct);
            if (emb is not null) embedded++;
        }

        _logger.LogInformation("Embedded {Count}/{Total} products", embedded, products.Count);
        return embedded;
    }

    /// <summary>
    /// Fuzzy keyword autocomplete for product titles and brand names.
    /// </summary>
    public async Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string query, int limit = 8, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return [];

        var normalized = query.ToLowerInvariant();

        var productSuggestions = await _db.Products
            .AsNoTracking()
            .Include(p => p.ProductType).ThenInclude(t => t!.Subcategory).ThenInclude(s => s.Category)
            .Where(p => p.Title.ToLower().Contains(normalized))
            .Select(p => new SearchSuggestionDto(
                p.Title,
                $"/products/{p.Id}",
                p.ProductType != null ? p.ProductType.Subcategory.Category.Name : (p.Category != null ? p.Category.Name : null)))
            .Take(limit)
            .ToListAsync(ct);

        if (productSuggestions.Count < limit)
        {
            var brandSuggestions = await _db.Brands
                .AsNoTracking()
                .Where(b => b.Name.ToLower().Contains(normalized))
                .Select(b => new SearchSuggestionDto(b.Name, $"/search?q={Uri.EscapeDataString(b.Name)}", "Brand"))
                .Take(limit - productSuggestions.Count)
                .ToListAsync(ct);
            productSuggestions.AddRange(brandSuggestions);
        }

        return productSuggestions;
    }

    private async Task<ProductEmbedding?> EmbedProductAsync(Product product, CancellationToken ct)
    {
        try
        {
            var text = BuildProductText(product);
            var response = await _aiInference.EmbedAsync(new EmbeddingsRequest(text), ct);
            var embedding = response.Embedding.Select(value => (float)value).ToArray();
            if (embedding.Length == 0) return null;

            var stored = await _db.ProductEmbeddings
                .FirstOrDefaultAsync(item => item.ProductId == product.Id, ct);
            if (stored is null)
            {
                stored = new ProductEmbedding { Id = Guid.NewGuid(), ProductId = product.Id };
                _db.ProductEmbeddings.Add(stored);
            }

            stored.Embedding = embedding;
            stored.Dimension = embedding.Length;
            stored.Model = response.Model;
            stored.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
            return stored;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error embedding indexed product {ProductId}", product.Id);
            return null;
        }
    }

    private static string BuildProductText(Product product)
        => string.Join(". ", new[]
        {
            product.Title,
            product.ShortDescription,
            product.Description,
            product.Keywords,
            product.Category?.Name,
            product.Origin,
            string.Join(", ", product.Tags),
            string.Join(", ", product.Ingredients)
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static double CosineSimilarity(float[] left, float[] right)
    {
        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;
        for (var index = 0; index < left.Length; index++)
        {
            dot += left[index] * right[index];
            leftNorm += left[index] * left[index];
            rightNorm += right[index] * right[index];
        }

        var denominator = Math.Sqrt(leftNorm * rightNorm);
        return denominator > 0 ? dot / denominator : double.NaN;
    }
}
