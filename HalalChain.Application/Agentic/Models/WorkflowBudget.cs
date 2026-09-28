namespace HalalChain.Application.Agentic.Models;

public sealed record WorkflowBudget(
    int MaxSteps = 20,
    int MaxTokens = 100_000,
    int MaxWallSeconds = 120,
    decimal MaxCostUsd = 1.00m);
