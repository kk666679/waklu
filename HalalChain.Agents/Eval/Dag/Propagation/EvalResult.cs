namespace HalalChain.Agents.Eval.Dag.Propagation;

using HalalChain.Agents.Eval.Taxonomy;

/// <summary>
/// Result of evaluating one node, after root-cause attribution.
///
/// This type must not declare a Verdict, ComplianceStatus, or HalalStatus
/// field. Evaluation reports whether an agent behaved as its goldens expect;
/// it has no authority over what that behavior means for compliance, and
/// that authority belongs to tawheed alone. Enforced by
/// EvaluationArchitectureTests.EvalResult_DeclaresNoVerdictField.
///
/// Note the terminal node is scored, not decided: comparing a terminal
/// outcome against a human label measures agent fidelity. It does not
/// produce the outcome.
/// </summary>
public sealed record EvalResult
{
    public required string TraceId { get; init; }
    public required string NodeId { get; init; }
    public required WorkflowNodeType NodeType { get; init; }

    /// <summary>Normalized to [0.0, 1.0]. Higher is better.</summary>
    public required double Score { get; init; }

    public required bool Passed { get; init; }

    public FailureCategory Category { get; init; } = FailureCategory.None;

    /// <summary>True when no failed ancestor explains this failure.</summary>
    public required bool IsRootCause { get; init; }

    /// <summary>The failed ancestor this failure was attributed to, if any.</summary>
    public string? PropagatedFrom { get; init; }

    public IReadOnlyDictionary<string, object?> Details { get; init; } =
        new Dictionary<string, object?>();
}
