namespace HalalChain.Agents.Runtime;

using System.Diagnostics;

using HalalChain.Application.Agentic.Abstractions;
using HalalChain.Application.Agentic.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Bounds a single agent workflow run.
///
/// A budget is a ceiling, not a suggestion. When steps run out the run is
/// stopped and the caller gets an exhausted budget, because a workflow that
/// keeps spending after its budget is gone is exactly the failure mode that
/// burns an LLM budget on a case that will never reach AUTONOMOUS confidence
/// anyway.
/// </summary>
public sealed class WorkflowBudgetLedger : IWorkflowBudget
{
    private readonly ILogger<WorkflowBudgetLedger> _logger;

    public WorkflowBudgetLedger(ILogger<WorkflowBudgetLedger>? logger = null) =>
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowBudgetLedger>.Instance;

    public IBudgetRun Begin(WorkflowBudget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);
        return new BudgetRun(budget, _logger);
    }

    internal sealed class BudgetRun : IBudgetRun
    {
        private readonly WorkflowBudget _budget;
        private readonly ILogger _logger;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private bool _disposed;

        public BudgetRun(WorkflowBudget budget, ILogger logger)
        {
            _budget = budget;
            _logger = logger;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public int StepsRemaining => Math.Max(0, _budget.MaxSteps - StepsUsed);

        public int StepsUsed { get; private set; }

        public bool IsExhausted =>
            StepsUsed >= _budget.MaxSteps ||
            _clock.Elapsed > TimeSpan.FromSeconds(_budget.MaxWallSeconds);

        /// <summary>
        /// Consumes one step, throwing if the budget is gone.
        ///
        /// Throwing rather than returning a flag is deliberate. A caller that
        /// ignores a flag runs unbounded; a caller that catches this exception
        /// has to decide what a truncated run means, which is the decision that
        /// should not be made silently.
        /// </summary>
        public void ConsumeStep()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_clock.Elapsed > TimeSpan.FromSeconds(_budget.MaxWallSeconds))
            {
                throw new WorkflowBudgetExhaustedException(
                    Id, $"Wall-clock budget of {_budget.MaxWallSeconds}s exhausted.");
            }

            if (StepsUsed >= _budget.MaxSteps)
            {
                throw new WorkflowBudgetExhaustedException(
                    Id, $"Step budget of {_budget.MaxSteps} exhausted.");
            }

            StepsUsed++;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _clock.Stop();

            if (IsExhausted)
            {
                _logger.LogWarning(
                    "Agent workflow run {RunId} exhausted its budget after {Steps} step(s) in {Elapsed:F1}s.",
                    Id, StepsUsed, _clock.Elapsed.TotalSeconds);
            }
        }
    }
}

public sealed class WorkflowBudgetExhaustedException(Guid runId, string detail)
    : InvalidOperationException($"Agent workflow run {runId}: {detail}")
{
    public Guid RunId { get; } = runId;
}

/// <summary>Options for the agent runtime client.</summary>
public sealed class AgentsRuntimeOptions
{
    public const string SectionName = "Agents";

    /// <summary>Base URL of the agents service, e.g. http://localhost:8081.</summary>
    public string BaseUrl { get; set; } = "http://localhost:8081";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Workflows this deployment will run. Anything else is rejected.</summary>
    public IList<string> AllowedWorkflows { get; set; } =
        ["supplier_onboarding", "certificate_review", "product_verification"];
}
