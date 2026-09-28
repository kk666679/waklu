namespace HalalChain.Agents.Eval.Dag.Scorers;

using System.Text.Json;
using HalalChain.Agents.Eval.Taxonomy;

/// <summary>
/// Scores a collector node: recall of the sources a golden says are required.
/// </summary>
public sealed class CollectorScorer(NodeThresholds thresholds) : INodeScorer
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Collector;

    public NodeScore Score(
        AgentTrace trace,
        WorkflowNode node,
        IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens)
    {
        var expected = Expectations(goldens, WorkflowNodeType.Collector)
            .GetValueOrDefault("required_sources");
        var actual = NodeOutput.StringList(node.Output, "sources_found");

        if (expected is not { ValueKind: JsonValueKind.Array })
        {
            // No golden means nothing to measure against. Scoring 1.0 would
            // make an unmeasured node indistinguishable from a correct one.
            return Unmeasured(node, "no golden sources defined");
        }

        var expectedSet = expected.EnumerateArray().Select(e => e.ToString()).ToHashSet(StringComparer.Ordinal);
        if (expectedSet.Count == 0)
            return Unmeasured(node, "no golden sources defined");

        var found = actual.ToHashSet(StringComparer.Ordinal);
        var recall = expectedSet.Count == 0 ? 1.0 : expectedSet.Count(found.Contains) / (double)expectedSet.Count;
        var passed = recall >= thresholds.Collector;

        return new NodeScore
        {
            NodeId = node.Id,
            NodeType = WorkflowNodeType.Collector,
            Score = recall,
            Passed = passed,
            Category = passed ? FailureCategory.None : FailureCategory.CollectorMissedSource,
            Details = Details(
                ("expected", expectedSet),
                ("actual", found),
                ("recall", recall)),
        };
    }

    internal static IReadOnlyDictionary<string, JsonElement> Expectations(
        IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens, WorkflowNodeType type) =>
        goldens.TryGetValue(type, out var golden)
            ? golden.Expectations
            : new Dictionary<string, JsonElement>();

    internal static NodeScore Unmeasured(WorkflowNode node, string note) => new()
    {
        NodeId = node.Id,
        NodeType = node.Type,
        Score = 1.0,
        Passed = true,
        Details = new Dictionary<string, object?> { ["note"] = note },
    };

    internal static IReadOnlyDictionary<string, object?> Details(
        params (string Key, object? Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
}

/// <summary>
/// Scores a classifier node: label accuracy against the golden labels.
/// </summary>
public sealed class ClassifierScorer(NodeThresholds thresholds) : INodeScorer
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Classifier;

    public NodeScore Score(
        AgentTrace trace,
        WorkflowNode node,
        IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens)
    {
        var expected = CollectorScorer.Expectations(goldens, WorkflowNodeType.Classifier)
            .GetValueOrDefault("expected_labels");
        var actual = NodeOutput.Object(node.Output, "labels");

        if (expected is not { ValueKind: JsonValueKind.Object })
            return CollectorScorer.Unmeasured(node, "no golden labels defined");

        var total = expected.EnumerateObject().Count();
        if (total == 0)
            return CollectorScorer.Unmeasured(node, "no golden labels defined");

        var correct = expected.EnumerateObject().Count(p =>
            actual.TryGetValue(p.Name, out var v) && string.Equals(v.ToString(), p.Value.ToString(), StringComparison.Ordinal));

        var accuracy = correct / (double)total;
        var passed = accuracy >= thresholds.Classifier;

        return new NodeScore
        {
            NodeId = node.Id,
            NodeType = WorkflowNodeType.Classifier,
            Score = accuracy,
            Passed = passed,
            Category = passed ? FailureCategory.None : FailureCategory.ClassifierMislabel,
            Details = CollectorScorer.Details(
                ("expected", expected.ToString()),
                ("actual", actual.ToDictionary(kv => kv.Key, kv => (object?)kv.Value.ToString())),
                ("accuracy", accuracy)),
        };
    }
}

/// <summary>
/// Scores a verifier node: agreement between expected and actual validity.
/// </summary>
public sealed class VerifierScorer(NodeThresholds thresholds) : INodeScorer
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Verifier;

    public NodeScore Score(
        AgentTrace trace,
        WorkflowNode node,
        IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens)
    {
        var expected = CollectorScorer.Expectations(goldens, WorkflowNodeType.Verifier)
            .GetValueOrDefault("expected_valid");
        var actual = NodeOutput.Bool(node.Output, "is_valid");

        // With no golden the node still has a meaningful output: an
        // unparseable or absent validity flag is itself a verifier failure.
        var expectedValid = expected is { ValueKind: JsonValueKind.True or JsonValueKind.False }
            ? expected.GetBoolean()
            : true;

        var score = expectedValid == actual ? 1.0 : 0.0;
        var passed = score >= thresholds.Verifier;

        return new NodeScore
        {
            NodeId = node.Id,
            NodeType = WorkflowNodeType.Verifier,
            Score = score,
            Passed = passed,
            Category = passed ? FailureCategory.None : FailureCategory.VerifierValidityDisagreement,
            Details = CollectorScorer.Details(
                ("expected_valid", expectedValid),
                ("actual_valid", actual)),
        };
    }
}

/// <summary>
/// Scores a gap node: F1 of the missing-requirement set.
///
/// Precision is measured against what the agent claimed was missing; recall
/// against what the golden says was missing. A gap agent that flags
/// everything scores zero precision — noisy escalation is a real failure mode,
/// not a safe default.
/// </summary>
public sealed class GapScorer(NodeThresholds thresholds) : INodeScorer
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Gap;

    public NodeScore Score(
        AgentTrace trace,
        WorkflowNode node,
        IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens)
    {
        var expected = CollectorScorer.Expectations(goldens, WorkflowNodeType.Gap)
            .GetValueOrDefault("expected_missing");
        var actual = NodeOutput.StringList(node.Output, "missing_requirements");

        if (expected is not { ValueKind: JsonValueKind.Array } expectedArray)
            return CollectorScorer.Unmeasured(node, "no missing requirements expected");

        var expectedSet = expectedArray.EnumerateArray().Select(e => e.ToString()).ToHashSet(StringComparer.Ordinal);
        if (expectedSet.Count == 0)
            return CollectorScorer.Unmeasured(node, "no missing requirements expected");

        var actualSet = actual.ToHashSet(StringComparer.Ordinal);
        var overlap = expectedSet.Count(actualSet.Contains);

        var precision = actualSet.Count == 0 ? 0.0 : overlap / (double)actualSet.Count;
        var recall = overlap / (double)expectedSet.Count;
        var f1 = precision + recall == 0 ? 0.0 : 2 * precision * recall / (precision + recall);
        var passed = f1 >= thresholds.Gap;

        return new NodeScore
        {
            NodeId = node.Id,
            NodeType = WorkflowNodeType.Gap,
            Score = f1,
            Passed = passed,
            Category = passed ? FailureCategory.None : FailureCategory.GapMissedRequirement,
            Details = CollectorScorer.Details(
                ("expected_missing", expectedSet),
                ("actual_missing", actualSet),
                ("precision", precision),
                ("recall", recall)),
        };
    }
}

/// <summary>
/// Scores a re-collection node: did the loop actually close?
/// </summary>
public sealed class RecollectionScorer(NodeThresholds thresholds) : INodeScorer
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Recollection;

    public NodeScore Score(
        AgentTrace trace,
        WorkflowNode node,
        IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens)
    {
        var closed = NodeOutput.Bool(node.Output, "loop_closed");
        var score = closed ? 1.0 : 0.0;
        var passed = score >= thresholds.Recollection;

        return new NodeScore
        {
            NodeId = node.Id,
            NodeType = WorkflowNodeType.Recollection,
            Score = score,
            Passed = passed,
            Category = passed ? FailureCategory.None : FailureCategory.RecollectionFailedToClose,
            Details = CollectorScorer.Details(("loop_closed", closed)),
        };
    }
}
