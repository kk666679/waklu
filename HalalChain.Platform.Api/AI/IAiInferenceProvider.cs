using HalalChain.Platform.Contracts.AI.Requests;
using HalalChain.Platform.Contracts.AI.Responses;

namespace HalalChain.Platform.Api.AI;

public interface IAiInferenceProvider
{
    Task<AiGatewayHealthResponse> HealthAsync(AiGatewayHealthRequest request, CancellationToken ct);
    Task<EmbeddingsResponse> EmbedAsync(EmbeddingsRequest request, CancellationToken ct);
    Task<SummarizeResponse> SummarizeAsync(SummarizeRequest request, CancellationToken ct);
    Task<ClassifyResponse> ClassifyAsync(ClassifyRequest request, CancellationToken ct);
    Task<IngredientParseResponse> IngredientParseAsync(IngredientParseRequest request, CancellationToken ct);
    Task<CertificateExtractResponse> CertificateExtractAsync(CertificateExtractRequest request, CancellationToken ct);
}
