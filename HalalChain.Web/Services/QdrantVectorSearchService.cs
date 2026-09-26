using HalalChain.Models;
using System.Text.Json.Serialization;

namespace HalalChain.Services;

/// <summary>
/// Vector search service using Qdrant for semantic search on product embeddings.
/// Enables finding semantically similar products and AI-powered discovery.
/// </summary>
public interface IQdrantVectorSearchService
{
    /// <summary>
    /// Generate product embedding and store in Qdrant.
    /// </summary>
    Task UpsertProductEmbeddingAsync(Product product, CancellationToken ct = default);

    /// <summary>
    /// Search for similar products by vector similarity.
    /// </summary>
    Task<List<SimilarProductResult>> SearchSimilarAsync(string query, int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Get embedding for a specific product ID.
    /// </summary>
    Task<ProductEmbedding?> GetProductEmbeddingAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Delete product embedding from Qdrant.
    /// </summary>
    Task DeleteProductEmbeddingAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Health check for Qdrant connectivity.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken ct = default);

    /// <summary>
    /// Initialize Qdrant collection if not exists.
    /// </summary>
    Task InitializeCollectionAsync(CancellationToken ct = default);
}

/// <summary>
/// Implementation of Qdrant-based vector search service.
/// </summary>
public class QdrantVectorSearchService : IQdrantVectorSearchService
{
    private readonly IProductService _productService;
    private readonly ILogger<QdrantVectorSearchService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _qdrantUrl;
    private readonly string _collectionName = "products";
    private readonly int _vectorSize = 384; // Dimension of embeddings (adjust based on model)

    public QdrantVectorSearchService(
        IProductService productService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<QdrantVectorSearchService> logger)
    {
        _productService = productService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _qdrantUrl = configuration["Qdrant:Url"] ?? "http://localhost:6333";
    }

    public async Task UpsertProductEmbeddingAsync(Product product, CancellationToken ct = default)
    {
        try
        {
            // Generate embedding from product data
            var embedding = await GenerateProductEmbeddingAsync(product, ct);

            // Prepare payload for Qdrant
            var payload = new QdrantUpsertRequest
            {
                Points = new List<QdrantPoint>
                {
                    new()
                    {
                        Id = (ulong)product.Id,
                        Vector = embedding.Embedding,
                        Payload = new Dictionary<string, object>
                        {
                            { "product_id", product.Id },
                            { "name", product.Name },
                            { "category_id", product.CategoryId },
                            { "price", product.Price },
                            { "rating", product.AverageRating },
                            { "is_halal", product.IsHalal },
                            { "tags", string.Join(",", product.Tags) },
                            { "updated_at", DateTime.UtcNow }
                        }
                    }
                }
            };

            // Upsert to Qdrant
            var endpoint = $"{_qdrantUrl}/collections/{_collectionName}/points?wait=true";
            var response = await _httpClient.PutAsJsonAsync(endpoint, payload, cancellationToken: ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Qdrant upsert failed: {StatusCode} {Error}", response.StatusCode, error);
                throw new InvalidOperationException($"Failed to upsert product embedding to Qdrant: {error}");
            }

            _logger.LogDebug("Product embedding upserted: ProductId={ProductId}, Name={Name}", product.Id, product.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting product embedding for ProductId: {ProductId}", product.Id);
            throw;
        }
    }

    public async Task<List<SimilarProductResult>> SearchSimilarAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        try
        {
            // Generate embedding for query
            var queryEmbedding = await GenerateQueryEmbeddingAsync(query, ct);

            // Search in Qdrant
            var searchRequest = new QdrantSearchRequest
            {
                Vector = queryEmbedding,
                Limit = limit,
                WithPayload = true,
                ScoreThreshold = 0.6 // Only return results above similarity threshold
            };

            var endpoint = $"{_qdrantUrl}/collections/{_collectionName}/points/search";
            var response = await _httpClient.PostAsJsonAsync(endpoint, searchRequest, cancellationToken: ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Qdrant search failed: {Error}", error);
                return new List<SimilarProductResult>();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var searchResponse = System.Text.Json.JsonSerializer.Deserialize<QdrantSearchResponse>(content);

            var results = searchResponse?.Result?
                .Select(r => new SimilarProductResult
                {
                    ProductId = (int)r.Id,
                    Similarity = r.Score,
                    Payload = r.Payload
                })
                .ToList() ?? new List<SimilarProductResult>();

            _logger.LogDebug("Semantic search completed: Query={Query}, Results={Count}", query, results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching similar products for query: {Query}", query);
            return new List<SimilarProductResult>();
        }
    }

    public async Task<ProductEmbedding?> GetProductEmbeddingAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            var product = await _productService.GetByIdAsync(productId, ct);
            if (product == null) return null;

            // In production, fetch from Qdrant point
            var embedding = await GenerateProductEmbeddingAsync(product, ct);
            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting embedding for ProductId: {ProductId}", productId);
            return null;
        }
    }

    public async Task DeleteProductEmbeddingAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            var deleteRequest = new QdrantDeleteRequest
            {
                PointIds = new List<ulong> { (ulong)productId }
            };

            var endpoint = $"{_qdrantUrl}/collections/{_collectionName}/points/delete?wait=true";
            var response = await _httpClient.PostAsJsonAsync(endpoint, deleteRequest, cancellationToken: ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to delete product embedding: ProductId={ProductId}", productId);
            }

            _logger.LogDebug("Product embedding deleted: ProductId={ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product embedding for ProductId: {ProductId}", productId);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var endpoint = $"{_qdrantUrl}/health";
            var response = await _httpClient.GetAsync(endpoint, cancellationToken: ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task InitializeCollectionAsync(CancellationToken ct = default)
    {
        try
        {
            // Check if collection exists
            var checkEndpoint = $"{_qdrantUrl}/collections/{_collectionName}";
            var checkResponse = await _httpClient.GetAsync(checkEndpoint, cancellationToken: ct);

            if (checkResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Qdrant collection already exists: {CollectionName}", _collectionName);
                return;
            }

            // Create collection
            var createRequest = new QdrantCollectionCreationRequest
            {
                Vectors = new VectorParams
                {
                    Size = _vectorSize,
                    Distance = "Cosine"
                }
            };

            var createEndpoint = $"{_qdrantUrl}/collections/{_collectionName}";
            var createResponse = await _httpClient.PutAsJsonAsync(createEndpoint, createRequest, cancellationToken: ct);

            if (!createResponse.IsSuccessStatusCode)
            {
                var error = await createResponse.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Failed to create Qdrant collection: {error}");
            }

            _logger.LogInformation("Qdrant collection created: {CollectionName}", _collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Qdrant collection");
            throw;
        }
    }

    // Private helpers

    private async Task<ProductEmbedding> GenerateProductEmbeddingAsync(Product product, CancellationToken ct = default)
    {
        // In production, call AI gateway to generate embeddings
        // For now, generate a placeholder deterministic embedding

        var combinedText = $"{product.Name} {product.Description} {string.Join(" ", product.Tags)} {string.Join(" ", product.Attributes)}";
        
        // Generate pseudo-random but deterministic embedding based on product ID
        var random = new Random(product.Id);
        var embedding = Enumerable.Range(0, _vectorSize)
            .Select(_ => (float)(random.NextDouble() * 2 - 1)) // Values between -1 and 1
            .ToList();

        // Normalize vector to unit length
        var magnitude = MathF.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            embedding = embedding.Select(x => x / magnitude).ToList();
        }

        return new ProductEmbedding
        {
            ProductId = product.Id,
            Embedding = embedding,
            GeneratedAt = DateTime.UtcNow,
            ModelVersion = "1.0"
        };
    }

    private async Task<List<float>> GenerateQueryEmbeddingAsync(string query, CancellationToken ct = default)
    {
        // In production, call AI gateway with query text
        // For now, generate a placeholder embedding

        var random = new Random(query.GetHashCode());
        var embedding = Enumerable.Range(0, _vectorSize)
            .Select(_ => (float)(random.NextDouble() * 2 - 1))
            .ToList();

        // Normalize
        var magnitude = MathF.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            embedding = embedding.Select(x => x / magnitude).ToList();
        }

        return embedding;
    }
}

// Qdrant API DTOs

public class ProductEmbedding
{
    public int ProductId { get; set; }
    public List<float> Embedding { get; set; } = [];
    public DateTime GeneratedAt { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
}

public class SimilarProductResult
{
    public int ProductId { get; set; }
    public float Similarity { get; set; }
    public Dictionary<string, object> Payload { get; set; } = [];
}

public class QdrantUpsertRequest
{
    [JsonPropertyName("points")]
    public List<QdrantPoint> Points { get; set; } = [];
}

public class QdrantPoint
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("vector")]
    public List<float> Vector { get; set; } = [];

    [JsonPropertyName("payload")]
    public Dictionary<string, object> Payload { get; set; } = [];
}

public class QdrantSearchRequest
{
    [JsonPropertyName("vector")]
    public List<float> Vector { get; set; } = [];

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 10;

    [JsonPropertyName("with_payload")]
    public bool WithPayload { get; set; } = true;

    [JsonPropertyName("score_threshold")]
    public float ScoreThreshold { get; set; } = 0.6f;
}

public class QdrantSearchResponse
{
    [JsonPropertyName("result")]
    public List<QdrantSearchResult>? Result { get; set; }
}

public class QdrantSearchResult
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("score")]
    public float Score { get; set; }

    [JsonPropertyName("payload")]
    public Dictionary<string, object> Payload { get; set; } = [];
}

public class QdrantDeleteRequest
{
    [JsonPropertyName("points")]
    public List<ulong> PointIds { get; set; } = [];
}

public class QdrantCollectionCreationRequest
{
    [JsonPropertyName("vectors")]
    public VectorParams Vectors { get; set; } = new();
}

public class VectorParams
{
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("distance")]
    public string Distance { get; set; } = "Cosine";
}
