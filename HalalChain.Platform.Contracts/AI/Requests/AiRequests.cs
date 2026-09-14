namespace HalalChain.Platform.Contracts.AI.Requests;

public sealed record AiGatewayHealthRequest;
public sealed record EmbeddingsRequest(string Text, string? Model = null);
public sealed record SummarizeRequest(string Text, int MaxTokens = 80);
public sealed record ClassifyRequest(string Text, string[] Labels);
public sealed record RerankRequest(string Query, string[] Passages);
public sealed record VectorizeRequest(string Text);

// New inference endpoints
public sealed record IngredientParseRequest(string Text);
public sealed record CertificateExtractRequest(string Text);

// AI shopping / catalog requests
public sealed record ShoppingSearchRequest(string Query, string? Jurisdiction = "MY", decimal? MaxPrice = null, string? Currency = "MYR", int Top = 10);
public sealed record ShoppingRecommendRequest(string ProductId, string? CustomerId = null, int Top = 5);
public sealed record CatalogEnrichRequest(string ProductId, string Title, string? Description = null, string[]? Ingredients = null);
public sealed record VendorCopilotRequest(string VendorId, string Message, string? Context = null);
public sealed record ChatRequest(string Message, string? Context = null, string Persona = "general");
