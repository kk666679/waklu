namespace HalalChain.Agents.Eval.Dag.Scorers;

using System.Diagnostics.CodeAnalysis;

using HalalChain.Agents.Eval.Taxonomy;

/// <summary>
/// Resolves a scorer per node type.
///
/// Every node type in <see cref="WorkflowNodeType"/> has a scorer, and
/// <see cref="GetRequired"/> throws rather than returning null. A node that
/// silently goes unscored is worse than a node that fails loudly: an
/// unevaluated node reads as a passing one in the report.
/// </summary>
public sealed class NodeScorerRegistry
{
    private readonly IReadOnlyDictionary<WorkflowNodeType, INodeScorer> _scorers;

    public NodeScorerRegistry(NodeThresholds? thresholds = null)
    {
        var t = thresholds ?? new NodeThresholds();

        _scorers = new Dictionary<WorkflowNodeType, INodeScorer>
        {
            [WorkflowNodeType.Collector] = new CollectorScorer(t),
            [WorkflowNodeType.Classifier] = new ClassifierScorer(t),
            [WorkflowNodeType.Verifier] = new VerifierScorer(t),
            [WorkflowNodeType.Handoff] = new HandoffScorer(),
            [WorkflowNodeType.Gap] = new GapScorer(t),
            [WorkflowNodeType.Recollection] = new RecollectionScorer(t),
            [WorkflowNodeType.Terminal] = new TerminalScorer(t),
        };
    }

    public IReadOnlyCollection<WorkflowNodeType> SupportedNodeTypes =>
        _scorers.Keys.ToArray();

    public bool TryGet(WorkflowNodeType type, [NotNullWhen(true)] out INodeScorer? scorer) =>
        _scorers.TryGetValue(type, out scorer);

    /// <summary>
    /// A trace node type with no scorer is a gap in the evaluation harness.
    /// Throwing is correct: it means the harness cannot measure something the
    /// workflow does, and that must be fixed before the report is trusted.
    /// </summary>
    public INodeScorer GetRequired(WorkflowNodeType type) =>
        _scorers.TryGetValue(type, out var scorer)
            ? scorer
            : throw new InvalidOperationException(
                $"No scorer registered for node type '{type}'. An unscoreable node type " +
                "means traces containing it cannot be evaluated; register a scorer " +
                "before treating an eval report as complete.");
}
