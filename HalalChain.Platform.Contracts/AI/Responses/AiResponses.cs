namespace HalalChain.Platform.Contracts.AI.Responses;

public sealed record AiGatewayHealthResponse(string Service, string Status, string UtcNow);
public sealed record EmbeddingsResponse(string Model, double[] Embedding);
public sealed record SummarizeResponse(string Model, string Summary);
public sealed record ClassifyResponse(string Model, string BestLabel, double BestScore, Dictionary<string, double> Scores);
public sealed record RerankResponse(string Model, RankedPassage[] Results);
public sealed record RankedPassage(int OriginalIndex, string Passage, double Score);

// Ingredient parse
public sealed record IngredientParseResponse(string Model, ParsedIngredient[] Parsed, IngredientSummary Summary);
public sealed record ParsedIngredient(string Name, double? Percentage, string? ECode, string Risk);
public sealed record IngredientSummary(int Total, int Halal, int Haram, int Mashbooh);

// Certificate extract
public sealed record CertificateExtractResponse(string Model, CertificateData Parsed, string Completeness);
public sealed record CertificateData(string? CertificateNumber, string? Issuer, string? Scope, string? Jurisdiction);

public sealed record ShoppingSearchResponse(ShoppingSearchResult[] Results, string Explanation);
public sealed record ShoppingSearchResult(string ProductId, string Name, string VendorName, decimal Price, string Currency, string HalalStatus, int Inventory);
public sealed record ShoppingRecommendResponse(ShoppingSearchResult[] Recommendations, string Explanation);
public sealed record CatalogEnrichResponse(string ProductId, string? SuggestedCategory, string? SuggestedDescription, string[] FlaggedIngredients, string[] Warnings);
public sealed record VendorCopilotResponse(string Reply, string[] SuggestedActions);
public sealed record ChatResponse(string Reply, SourceItem[]? Sources = null, string[]? Reasoning = null, string[]? Suggestions = null, List<ToolCallInfo>? ToolCalls = null);
public sealed record SourceItem(string Title, string Url);
public sealed record ToolCallInfo(string ToolName, string Status = "completed", object? Parameters = null, string? Result = null);
