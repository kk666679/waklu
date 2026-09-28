namespace HalalChain.Application.Agentic.Abstractions;

using HalalChain.Application.Agentic.Models;

public interface IWorkflowBudget
{
    IBudgetRun Begin(WorkflowBudget budget);
}

public interface IBudgetRun : IDisposable
{
    Guid Id { get; }
    int StepsRemaining { get; }
    bool IsExhausted { get; }
}
