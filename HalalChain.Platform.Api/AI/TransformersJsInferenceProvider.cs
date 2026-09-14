using System.Net.Http.Headers;
using System.Net.Http.Json;
using HalalChain.Platform.Contracts.AI.Requests;
using HalalChain.Platform.Contracts.AI.Responses;
using Microsoft.Extensions.Options;

namespace HalalChain.Platform.Api.AI;

public sealed class TransformersJsInferenceProvider : IAiInferenceProvider
{
    private readonly HttpClient _http;
    private readonly AiGatewayOptions _options;

    public TransformersJsInferenceProvider(HttpClient http, IOptions<AiGatewayOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public Task<AiGatewayHealthResponse> HealthAsync(AiGatewayHealthRequest request, CancellationToken ct)
        => PostAsync<AiGatewayHealthResponse>("/health", request, ct);

    public Task<EmbeddingsResponse> EmbedAsync(EmbeddingsRequest request, CancellationToken ct)
        => PostAsync<EmbeddingsResponse>("/embeddings", request, ct);

    public Task<SummarizeResponse> SummarizeAsync(SummarizeRequest request, CancellationToken ct)
        => PostAsync<SummarizeResponse>("/summarize", request, ct);

    public Task<ClassifyResponse> ClassifyAsync(ClassifyRequest request, CancellationToken ct)
        => PostAsync<ClassifyResponse>("/classify", request, ct);

    public Task<IngredientParseResponse> IngredientParseAsync(IngredientParseRequest request, CancellationToken ct)
        => PostAsync<IngredientParseResponse>("/ingredient-parse", request, ct);

    public Task<CertificateExtractResponse> CertificateExtractAsync(CertificateExtractRequest request, CancellationToken ct)
        => PostAsync<CertificateExtractResponse>("/certificate-extract", request, ct);

    private async Task<TResponse> PostAsync<TResponse>(string path, object payload, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };
        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && _http.DefaultRequestHeaders.Authorization is null)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct)
               ?? throw new InvalidOperationException($"Null response from AI gateway for {typeof(TResponse).Name}.");
    }
}
