using HalalChain.Application.Policies;
using HalalChain.Platform.Api.AI;
using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Contracts.AI.Requests;
using HalalChain.Platform.Contracts.AI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.AI;

[ApiController]
public sealed class AiController(IAiInferenceProvider provider, HalalChainDbContext db) : ControllerBase
{
    // ── Infrastructure ────────────────────────────────────────────────────

    [HttpPost("/api/ai/health")]
    [AllowAnonymous]
    public Task<AiGatewayHealthResponse> Health([FromBody] AiGatewayHealthRequest request, CancellationToken ct)
        => provider.HealthAsync(request, ct);

    [HttpPost("/api/ai/embeddings")]
    [Authorize(Policy = CatalogPolicies.VendorOnly)]
    public Task<EmbeddingsResponse> Embeddings([FromBody] EmbeddingsRequest request, CancellationToken ct)
        => provider.EmbedAsync(request, ct);

    [HttpPost("/api/ai/summarize")]
    [Authorize(Policy = CatalogPolicies.VendorOnly)]
    public Task<SummarizeResponse> Summarize([FromBody] SummarizeRequest request, CancellationToken ct)
        => provider.SummarizeAsync(request, ct);

    [HttpPost("/api/ai/classify")]
    [Authorize(Policy = CatalogPolicies.VendorOnly)]
    public Task<ClassifyResponse> Classify([FromBody] ClassifyRequest request, CancellationToken ct)
        => provider.ClassifyAsync(request, ct);

    [HttpPost("/api/ai/ingredient-parse")]
    [Authorize(Policy = CatalogPolicies.VendorOnly)]
    public Task<IngredientParseResponse> IngredientParse([FromBody] IngredientParseRequest request, CancellationToken ct)
        => provider.IngredientParseAsync(request, ct);

    [HttpPost("/api/ai/certificate-extract")]
    [Authorize(Policy = CatalogPolicies.VendorOnly)]
    public Task<CertificateExtractResponse> CertificateExtract([FromBody] CertificateExtractRequest request, CancellationToken ct)
        => provider.CertificateExtractAsync(request, ct);

    // ── Shopping AI — catalog-grounded ──────────────────────────────────

    [HttpPost("/api/ai/shopping/search")]
    [Authorize(Policy = CatalogPolicies.VendorOnly)]
    public async Task<ActionResult<ShoppingSearchResponse>> ShoppingSearch(
        [FromBody] ShoppingSearchRequest request, CancellationToken ct)
    {
        var q = db.Products
            .Include(p => p.Category).Include(p => p.Vendor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Jurisdiction))
            q = q.Where(p => p.Origin == request.Jurisdiction || p.Vendor!.Country == request.Jurisdiction);

        if (request.MaxPrice.HasValue)
            q = q.Where(p => p.Price <= request.MaxPrice.Value);

        var results = await q
            .OrderBy(p => p.Title)
            .Take(request.Top)
            .Select(p => new ShoppingSearchResult(
                p.Id.ToString(), p.Title, p.Vendor!.Name, p.Price, p.Currency,
                p.Certificates.Any(c => c.Status == CertificateStatus.Verified) ? "Verified" : "Unverified",
                p.Inventory))
            .ToArrayAsync(ct);

        return Ok(new ShoppingSearchResponse(results, $"Found {results.Length} products."));
    }

    [HttpPost("/api/ai/shopping/recommend")]
    [Authorize(Policy = CatalogPolicies.VendorOnly)]
    public async Task<ActionResult<ShoppingRecommendResponse>> Recommend(
        [FromBody] ShoppingRecommendRequest request, CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Category).Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.Id.ToString() == request.ProductId, ct);

        if (product is null)
            return Ok(new ShoppingRecommendResponse([], "Product not found."));

        var similar = await db.Products
            .Include(p => p.Vendor)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.Inventory > 0)
            .OrderBy(p => p.Price)
            .Take(request.Top)
            .Select(p => new ShoppingSearchResult(
                p.Id.ToString(), p.Title, p.Vendor!.Name, p.Price, p.Currency,
                p.Certificates.Any(c => c.Status == CertificateStatus.Verified) ? "Verified" : "Unverified",
                p.Inventory))
            .ToArrayAsync(ct);

        return Ok(new ShoppingRecommendResponse(similar, $"Recommended {similar.Length} products from the same category."));
    }

    // ── Catalog AI ────────────────────────────────────────────────────────

    [HttpPost("/api/ai/catalog/enrich")]
    [Authorize(Policy = CatalogPolicies.VendorOnly)]
    public async Task<ActionResult<CatalogEnrichResponse>> Enrich(
        [FromBody] CatalogEnrichRequest request, CancellationToken ct)
    {
        var ingredients = request.Ingredients ?? [];
        var flagged = new List<string>();
        var warnings = new List<string>();

        if (ingredients.Length > 0)
        {
            var classify = await provider.ClassifyAsync(
                new ClassifyRequest(string.Join(", ", ingredients),
                    ["halal", "haram", "mashbooh", "unknown"]), ct);
            if (classify.BestLabel is "haram" or "mashbooh")
            {
                flagged.AddRange(ingredients);
                warnings.Add($"Ingredient classification signal: {classify.BestLabel} (score {classify.BestScore:F2}). Requires human verification.");
            }
        }

        return Ok(new CatalogEnrichResponse(request.ProductId, null, null, [.. flagged], [.. warnings]));
    }

    // ── Vendor Copilot ────────────────────────────────────────────────────

    [HttpPost("/api/ai/vendor/copilot")]
    [Authorize(Policy = CatalogPolicies.VendorOnly)]
    public async Task<ActionResult<VendorCopilotResponse>> VendorCopilot(
        [FromBody] VendorCopilotRequest request, CancellationToken ct)
    {
        var summary = await provider.SummarizeAsync(new SummarizeRequest(request.Message, 120), ct);
        return Ok(new VendorCopilotResponse(
            $"Vendor copilot received: {summary.Summary}",
            ["Upload halal certificate", "Update product description", "Check inventory levels"]));
    }
}
