using HalalChain.Application.Tawheed.Models;

namespace HalalChain.Application.Tawheed;

/// <summary>
/// The only way Application reaches a policy decision. Everything else in
/// the system that needs to know whether a case is halal goes through this.
///
/// There is no in-process fallback. If tawheed is unreachable, the call
/// fails — the system does not guess, and it does not evaluate policy
/// locally. That is the architecture's central claim made structural.
/// </summary>
public interface ITawheedClient
{
    Task<PolicyEvaluationResponse> EvaluateAsync(
        PolicyEvaluationRequest request,
        CancellationToken ct = default);

    Task<PolicyVersion> GetCurrentPolicyVersionAsync(CancellationToken ct = default);
}
