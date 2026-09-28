namespace HalalChain.Agents.Eval.Dag;

using HalalChain.Agents.Eval.Taxonomy;

/// <summary>
/// Score for a single workflow node, produced before root-cause attribution.
///
/// A node score is a measurement against a golden expectation. It says
/// "the collector found 3 of 4 expected sources", not "the product is
/// compliant". Attributing a score to a root cause happens later, in
/// <see cref="Propagation.GreedyParentAttribution"/>.
/// </summary>
public sealed record NodeScore
{
    public required string NodeId { get; init; }
    public required WorkflowNodeType NodeType { get; init; }

    /// <summary>Normalized to [0.0, 1.0]. Higher is better.</summary>
    public required double Score { get; init; }

    public required bool Passed { get; init; }

    public FailureCategory Category { get; init; } = FailureCategory.None;

    public IReadOnlyDictionary<string, object?> Details { get; init; } =
        new Dictionary<string, object?>();
}
