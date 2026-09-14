using HalalChain.Platform.Contracts.Catalog.Dto;
using Microsoft.AspNetCore.Mvc;

namespace HalalChain.Platform.Api.Modules.Catalog;

[ApiController]
[Route("api/v1/search")]
[ApiVersion("1.0")]
public sealed class SearchController : ControllerBase
{
    private readonly SemanticSearchService _searchService;

    public SearchController(SemanticSearchService searchService) => _searchService = searchService;

    /// <summary>
    /// Semantic product search using AI embeddings and cosine similarity.
    /// </summary>
    [HttpGet("semantic")]
    public async Task<ActionResult<List<SemanticSearchResult>>> SemanticSearch(
        [FromQuery] string q,
        [FromQuery] int topK = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query parameter 'q' is required." });

        var results = await _searchService.SearchAsync(q, topK, ct);
        return Ok(results);
    }

    /// <summary>
    /// Generate embeddings for all products in the catalog.
    /// </summary>
    [HttpPost("embed-all")]
    public async Task<ActionResult> EmbedAll(CancellationToken ct)
    {
        var count = await _searchService.EmbedAllProductsAsync(ct);
        return Ok(new { embedded = count, message = $"Embedded {count} products." });
    }

    /// <summary>
    /// Generate embedding for a specific product.
    /// </summary>
    [HttpPost("embed/{productId:guid}")]
    public async Task<ActionResult> EmbedProduct(Guid productId, [FromBody] EmbedProductRequest request, CancellationToken ct)
    {
        var result = await _searchService.EmbedProductAsync(productId, request.Text, ct);
        if (result is null) return NotFound(new { error = "Failed to embed product." });
        return Ok(new { productId, dimension = result.Dimension, model = result.Model });
    }

    /// <summary>
    /// Fuzzy keyword autocomplete for the marketplace search nav.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<List<SearchSuggestionDto>>> GetSuggestions(
        [FromQuery] string q,
        [FromQuery] int limit = 8,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new List<SearchSuggestionDto>());

        var suggestions = await _searchService.GetSuggestionsAsync(q, limit, ct);
        return Ok(suggestions);
    }
}

public sealed record EmbedProductRequest(string Text);
