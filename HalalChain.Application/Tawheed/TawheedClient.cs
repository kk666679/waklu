namespace HalalChain.Application.Tawheed;

using System.Net.Http.Json;
using HalalChain.Application.Tawheed.Models;

/// <summary>
/// HTTP client to the tawheed service. Delegates all policy evaluation.
/// No local evaluation. No caching of verdicts — a verdict is only valid
/// as of the moment tawheed issued it.
/// </summary>
public sealed class TawheedClient : ITawheedClient
{
    private readonly HttpClient _http;

    public TawheedClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PolicyEvaluationResponse> EvaluateAsync(
        PolicyEvaluationRequest request,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/v1/policy/evaluate", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PolicyEvaluationResponse>(ct))!;
    }

    public async Task<PolicyVersion> GetCurrentPolicyVersionAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/v1/policy/version", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PolicyVersion>(ct))!;
    }
}
