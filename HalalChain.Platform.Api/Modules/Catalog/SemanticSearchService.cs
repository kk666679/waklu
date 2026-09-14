using System.Net.Http.Json;
using System.Text.Json;
using HalalChain.Domain.Catalog;
using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Contracts.Catalog.Dto;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Catalog;

/// <summary>
/// Semantic search service using pgvector-style cosine similarity.
/// Generates embeddings via the ai-inference gateway, stores them in PostgreSQL,
/// and performs vector similarity search using raw SQL.
/// 
/// The ai-inference gateway produces 256-dim embeddings. These are stored as
/// real[] columns in PostgreSQL. Cosine similarity is computed using:
///   similarity = 1 - (embedding <=> query_embedding)
/// where <=> is the cosine distance operator.
/// </summary>
public sealed class SemanticSearchService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HalalChainDbContext _db;
    private readonly ILogger<SemanticSearchService> _logger;
    private readonly IConfiguration _configuration;

    public SemanticSearchService(
        IHttpClientFactory httpClientFactory,
        HalalChainDbContext db,
        ILogger<SemanticSearchService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _db = db;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Generate embedding for a product and store it.
    /// </summary>
    public async Task<ProductEmbedding?> EmbedProductAsync(Guid productId, string text, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AiInference");
            var response = await client.PostAsJsonAsync("/embeddings", new { text }, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to generate embedding for product {ProductId}", productId);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResult>(cancellationToken: ct);
            if (result?.Embedding is null || result.Embedding.Length == 0) return null;

            // Upsert embedding
            var existing = await _db.ProductEmbeddings.FirstOrDefaultAsync(pe => pe.ProductId == productId, ct);
            if (existing is not null)
            {
                existing.Embedding = result.Embedding;
                existing.Dimension = result.Embedding.Length;
                existing.Model = result.Model ?? "halalchain-local-v1";
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                existing = new ProductEmbedding
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    Embedding = result.Embedding,
                    Dimension = result.Embedding.Length,
                    Model = result.Model ?? "halalchain-local-v1",
                };
                _db.ProductEmbeddings.Add(existing);
            }
            await _db.SaveChangesAsync(ct);
            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error embedding product {ProductId}", productId);
            return null;
        }
    }

    /// <summary>
    /// Search products by semantic similarity using cosine distance.
    /// Uses raw SQL for optimal PostgreSQL vector performance.
    /// </summary>
    public async Task<List<SemanticSearchResult>> SearchAsync(string query, int topK = 10, CancellationToken ct = default)
    {
        // Generate query embedding
        var client = _httpClientFactory.CreateClient("AiInference");
        var response = await client.PostAsJsonAsync("/embeddings", new { text = query }, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to generate query embedding");
            return [];
        }

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResult>(cancellationToken: ct);
        if (result?.Embedding is null || result.Embedding.Length == 0) return [];

        // Cosine similarity search using raw SQL
        // PostgreSQL array distance: 1 - (a <=> b) = cosine similarity
        var connStr = _configuration.GetConnectionString("Postgres");
        if (string.IsNullOrEmpty(connStr)) return [];

        var queryEmbedding = result.Embedding;
        var results = new List<SemanticSearchResult>();

        await using var conn = new Npgsql.NpgsqlConnection(connStr);
        await conn.OpenAsync(ct);

        // Create pgvector extension if not exists, then search
        var sql = @"
            CREATE EXTENSION IF NOT EXISTS vector;
            
            WITH query AS (
                SELECT ARRAY[{0}]::real[] AS q
            )
            SELECT 
                pe.""ProductId"",
                p.""Title"",
                p.""Slug"",
                p.""Description"",
                v.""Name"" AS ""VendorName"",
                c.""Name"" AS ""CategoryName"",
                p.""Price"",
                p.""Currency"",
                p.""Origin"",
                1 - (pe.""Embedding"" <=> (SELECT q FROM query)) AS ""Similarity""
            FROM ""ProductEmbeddings"" pe
            JOIN ""Products"" p ON pe.""ProductId"" = p.""Id""
            JOIN ""Vendors"" v ON p.""VendorId"" = v.""Id""
            JOIN ""Categories"" c ON p.""CategoryId"" = c.""Id""
            ORDER BY pe.""Embedding"" <=> (SELECT q FROM query)
            LIMIT {1}";

        var embeddingStr = string.Join(",", queryEmbedding.Select(x => x.ToString("F6")));
        var cmd = new Npgsql.NpgsqlCommand(string.Format(sql, embeddingStr, topK), conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new SemanticSearchResult(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetDecimal(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetDouble(9)));
        }

        return results;
    }

    /// <summary>
    /// Generate embeddings for all products in the catalog.
    /// </summary>
    public async Task<int> EmbedAllProductsAsync(CancellationToken ct = default)
    {
        var products = await _db.Products.ToListAsync(ct);
        var embedded = 0;

        foreach (var product in products)
        {
            var text = $"{product.Title}. {product.Description ?? ""}. Category: {product.Category?.Name ?? ""}. Origin: {product.Origin}.";
            var emb = await EmbedProductAsync(product.Id, text, ct);
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

    private sealed record EmbeddingResult(float[] Embedding, string? Model);
}
