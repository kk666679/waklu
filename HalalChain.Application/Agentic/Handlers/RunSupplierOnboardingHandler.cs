namespace HalalChain.Application.Agentic.Handlers;

using HalalChain.Application.Agentic.Abstractions;
using HalalChain.Application.Agentic.Models;
using HalalChain.Application.Tawheed;
using HalalChain.Application.Tawheed.Models;
using MediatR;

/// <summary>
/// Orchestrates the supplier_onboarding workflow.
///
/// This handler does not call agents directly — it delegates to
/// IAgentWorkflow, whose Python implementation runs the agent loop. The
/// handler's job is sequencing: gather proposals, hand them to tawheed,
/// and emit the resulting binding.
///
/// The handler never inspects evidence to decide. It calls tawheed.
/// </summary>
public sealed class RunSupplierOnboardingHandler
    : IRequestHandler<RunSupplierOnboardingCommand, SupplierOnboardingResult>
{
    private readonly IAgentWorkflow _agents;
    private readonly ITawheedClient _tawheed;
    private readonly IWorkflowBudget _budget;

    public RunSupplierOnboardingHandler(
        IAgentWorkflow agents,
        ITawheedClient tawheed,
        IWorkflowBudget budget)
    {
        _agents = agents;
        _tawheed = tawheed;
        _budget = budget;
    }

    public async Task<SupplierOnboardingResult> Handle(
        RunSupplierOnboardingCommand request,
        CancellationToken ct)
    {
        using var run = _budget.Begin(request.Budget);

        var proposals = await _agents.RunAsync(
            workflow: "supplier_onboarding",
            input: new { request.VendorId },
            ct: ct);

        var evaluation = await _tawheed.EvaluateAsync(new PolicyEvaluationRequest
        {
            VendorId = request.VendorId,
            Evidence = proposals.Select(p => p.ProposedEvidence).ToList(),
        }, ct);

        return new SupplierOnboardingResult(
            VendorId: request.VendorId,
            Verdict: evaluation.Verdict,
            Gaps: evaluation.Gaps,
            ProposalCount: proposals.Count,
            WorkflowRunId: run.Id,
            CompletedAt: DateTimeOffset.UtcNow);
    }
}

public sealed record RunSupplierOnboardingCommand(
    Guid VendorId,
    WorkflowBudget Budget) : IRequest<SupplierOnboardingResult>;

public sealed record SupplierOnboardingResult(
    Guid VendorId,
    Verdict Verdict,
    IReadOnlyList<PolicyGap> Gaps,
    int ProposalCount,
    Guid WorkflowRunId,
    DateTimeOffset CompletedAt);
