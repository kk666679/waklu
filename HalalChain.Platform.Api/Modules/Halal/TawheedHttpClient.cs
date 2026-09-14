using System.Net.Http.Json;

namespace HalalChain.Platform.Api.Modules.Halal;

/// <summary>
/// HTTP client for the Tawheed multi-agent verification system.
/// Sends verification requests to the Tawheed API and processes agent results
/// through the deterministic Policy Engine.
/// </summary>
public sealed class TawheedHttpClient : ITawheedClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TawheedHttpClient> _logger;

    public TawheedHttpClient(HttpClient http, ILogger<TawheedHttpClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<TawheedVerificationResult> RequestVerificationAsync(
        TawheedVerificationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Requesting Tawheed verification for product {ProductId} ({Title}), jurisdiction={Jurisdiction}",
                request.ProductId, request.ProductTitle, request.Jurisdiction);

            var payload = new
            {
                product_id = request.ProductId.ToString(),
                product_title = request.ProductTitle,
                description = request.Description,
                ingredients = request.Ingredients,
                certificate_number = request.CertificateNumber,
                certification_body = request.CertificationBody,
                jurisdiction = request.Jurisdiction,
                policy_version = request.PolicyVersion
            };

            var response = await _http.PostAsJsonAsync("/api/v1/verify", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Tawheed returned {StatusCode}: {Error}", response.StatusCode, error);
                return new TawheedVerificationResult
                {
                    Success = false,
                    Status = "Failed",
                    ErrorMessage = $"Tawheed returned {(int)response.StatusCode}: {error}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<TawheedApiResponse>(cancellationToken: ct);

            return new TawheedVerificationResult
            {
                Success = true,
                Status = result?.Status ?? "Completed",
                ComplianceStatus = result?.ComplianceStatus,
                RiskScore = result?.RiskScore ?? 0,
                ReasonCodes = result?.ReasonCodes ?? [],
                MissingEvidence = result?.MissingEvidence ?? [],
                RequiresHumanReview = result?.RequiresHumanReview ?? false,
                AgentResults = result?.Agents?.Select(a => new TawheedAgentResult
                {
                    AgentType = a.Type ?? "",
                    Status = a.Status ?? "",
                    Confidence = a.Confidence,
                    EvidenceTypes = a.EvidenceTypes ?? [],
                    Summary = a.Summary
                }).ToArray() ?? []
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Tawheed at {BaseUrl}", _http.BaseAddress);
            return new TawheedVerificationResult
            {
                Success = false,
                Status = "Failed",
                ErrorMessage = $"Connection failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Tawheed verification for {ProductId}", request.ProductId);
            return new TawheedVerificationResult
            {
                Success = false,
                Status = "Failed",
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    private sealed class TawheedApiResponse
    {
        public string? Status { get; set; }
        public string? ComplianceStatus { get; set; }
        public double RiskScore { get; set; }
        public string[]? ReasonCodes { get; set; }
        public string[]? MissingEvidence { get; set; }
        public bool RequiresHumanReview { get; set; }
        public AgentResponse[]? Agents { get; set; }
    }

    private sealed class AgentResponse
    {
        public string? Type { get; set; }
        public string? Status { get; set; }
        public double Confidence { get; set; }
        public string[]? EvidenceTypes { get; set; }
        public string? Summary { get; set; }
    }
}
