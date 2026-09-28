using HalalChain.Agents.Eval.Dag;
using HalalChain.Agents.Eval.Dag.Propagation;
using HalalChain.Agents.Eval.Taxonomy;

using Xunit;

namespace HalalChain.Agents.Tests;

public sealed class GreedyParentAttributionTests
{
    private static NodeScore Fail(string id, WorkflowNodeType type, FailureCategory category = FailureCategory.None) => new()
    {
        NodeId = id,
        NodeType = type,
        Score = 0.0,
        Passed = false,
        Category = category,
    };

    private static NodeScore Pass(string id, WorkflowNodeType type) => new()
    {
        NodeId = id,
        NodeType = type,
        Score = 1.0,
        Passed = true,
    };

    private static AgentTrace Trace(
        (string Id, WorkflowNodeType Type)[] nodes, TraceEdge[]? edges = null) => new()
    {
        TraceId = "t1",
        WorkflowName = "supplier_onboarding",
        Nodes = nodes.Select(n => new WorkflowNode { Id = n.Id, Type = n.Type }).ToArray(),
        Edges = edges ?? [],
        StartedAt = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public void IndependentFailures_AreEachRootCauses()
    {
        var trace = Trace([("c", WorkflowNodeType.Collector), ("g", WorkflowNodeType.Gap)]);
        var results = GreedyParentAttribution.Apply(trace, [Fail("c", WorkflowNodeType.Collector), Fail("g", WorkflowNodeType.Gap)]);

        Assert.All(results, r => Assert.True(r.IsRootCause));
        Assert.All(results, r => Assert.Null(r.PropagatedFrom));
    }

    [Fact]
    public void DownstreamFailure_IsAttributedToNearestFailedAncestor()
    {
        // collector -> handoff: the collector is the real problem.
        var trace = Trace(
            [("c", WorkflowNodeType.Collector),
             ("h", WorkflowNodeType.Handoff)],
            [new TraceEdge("c", "h")]);

        var results = GreedyParentAttribution.Apply(trace, [Fail("c", WorkflowNodeType.Collector), Fail("h", WorkflowNodeType.Handoff)]);

        var collector = results.Single(r => r.NodeId == "c");
        var handoff = results.Single(r => r.NodeId == "h");

        Assert.True(collector.IsRootCause);
        Assert.Null(collector.PropagatedFrom);

        Assert.False(handoff.IsRootCause);
        Assert.Equal("c", handoff.PropagatedFrom);
    }

    [Fact]
    public void TransitiveChain_AttributesToTheFailedAncestorDirectlyAbove()
    {
        // collector -> verifier -> handoff, all failed. Greedy-parent takes
        // the nearest failed ancestor, matching the Python implementation:
        // handoff blames verifier (the step that actually broke next), and
        // verifier blames collector. This matches the AgentEval strategy —
        // it is not a transitive walk to the earliest failure.
        var trace = Trace(
            [("c", WorkflowNodeType.Collector),
             ("v", WorkflowNodeType.Verifier),
             ("h", WorkflowNodeType.Handoff)],
            [new TraceEdge("c", "v"), new TraceEdge("v", "h")]);

        var results = GreedyParentAttribution.Apply(trace, [
            Fail("c", WorkflowNodeType.Collector),
            Fail("v", WorkflowNodeType.Verifier),
            Fail("h", WorkflowNodeType.Handoff)]);

        Assert.Equal("c", results.Single(r => r.NodeId == "v").PropagatedFrom);
        Assert.Equal("v", results.Single(r => r.NodeId == "h").PropagatedFrom);
        Assert.Equal(1, results.Count(r => r.IsRootCause));
    }

    [Fact]
    public void PassedAncestor_DoesNotBreakTheChain()
    {
        // A passing intermediate node must not prevent attribution to the
        // failed node above it — otherwise a single green step would make
        // every downstream failure look like an independent root cause.
        var trace = Trace(
            [("c", WorkflowNodeType.Collector),
             ("v", WorkflowNodeType.Verifier),
             ("h", WorkflowNodeType.Handoff)],
            [new TraceEdge("c", "v"), new TraceEdge("v", "h")]);

        var results = GreedyParentAttribution.Apply(trace, [
            Fail("c", WorkflowNodeType.Collector),
            Pass("v", WorkflowNodeType.Verifier),
            Fail("h", WorkflowNodeType.Handoff)]);

        Assert.Equal("c", results.Single(r => r.NodeId == "h").PropagatedFrom);
        Assert.Equal(1, results.Count(r => r.IsRootCause));
    }

    [Fact]
    public void CycleInTrace_StillResolvesExactlyOneRootCause()
    {
        // A malformed shadow-mode trace can contain an edge cycle. Two
        // consequences matter: the walk must terminate, and it must still name
        // a root cause. A naive backward walk in a cycle makes every failed
        // node point at another failed node, so nothing is a root and the
        // report says "failures, cause unknown" — the least actionable output
        // possible.
        var trace = Trace(
            [("a", WorkflowNodeType.Collector),
             ("b", WorkflowNodeType.Classifier)],
            [new TraceEdge("a", "b"), new TraceEdge("b", "a")]);

        var results = GreedyParentAttribution.Apply(trace, [Fail("a", WorkflowNodeType.Collector), Fail("b", WorkflowNodeType.Classifier)]);

        Assert.Equal(2, results.Count);
        Assert.Equal(1, results.Count(r => r.IsRootCause));
    }

    [Fact]
    public void CycleInTrace_ResolvesTheSameRootEveryRun()
    {
        // A regression signal is only useful if it is stable. The tie-break
        // must not depend on dictionary or scheduling order.
        var trace = Trace(
            [("a", WorkflowNodeType.Collector),
             ("b", WorkflowNodeType.Classifier)],
            [new TraceEdge("a", "b"), new TraceEdge("b", "a")]);

        var scores = new[] { Fail("a", WorkflowNodeType.Collector), Fail("b", WorkflowNodeType.Classifier) };

        var first = GreedyParentAttribution.Apply(trace, scores).Single(r => r.IsRootCause).NodeId;
        var second = GreedyParentAttribution.Apply(trace, scores).Single(r => r.IsRootCause).NodeId;

        Assert.Equal(first, second);
    }

    [Fact]
    public void SelfEdge_IsIgnored()
    {
        var trace = Trace([("a", WorkflowNodeType.Collector)], [new TraceEdge("a", "a")]);

        var results = GreedyParentAttribution.Apply(trace, [Fail("a", WorkflowNodeType.Collector)]);

        var a = Assert.Single(results);
        Assert.True(a.IsRootCause);
    }

    [Fact]
    public void EdgeNamingUnknownNode_IsIgnoredRatherThanThrowing()
    {
        // Shadow mode reads traces a partially-written agent may still be
        // emitting. One dangling edge must not abort the whole evaluation.
        var trace = Trace(
            [("a", WorkflowNodeType.Collector)],
            [new TraceEdge("a", "does-not-exist")]);

        var results = GreedyParentAttribution.Apply(trace, [Fail("a", WorkflowNodeType.Collector)]);

        Assert.True(Assert.Single(results).IsRootCause);
    }

    [Fact]
    public void AggregateTaxonomy_CountsOnlyRootCauses()
    {
        var trace = Trace(
            [("c", WorkflowNodeType.Collector),
             ("h", WorkflowNodeType.Handoff)],
            [new TraceEdge("c", "h")]);

        var results = GreedyParentAttribution.Apply(trace, [
            Fail("c", WorkflowNodeType.Collector, FailureCategory.CollectorMissedSource),
            Fail("h", WorkflowNodeType.Handoff, FailureCategory.HandoffMissingRequiredField)]);

        var taxonomy = GreedyParentAttribution.AggregateTaxonomy(results);

        // One real problem (collector), not two.
        var entry = Assert.Single(taxonomy);
        Assert.Equal(FailureCategory.CollectorMissedSource, entry.Key);
        Assert.Equal(1, entry.Value);
    }

    [Fact]
    public void PassingNodes_CarryNoCategory()
    {
        var trace = Trace([("c", WorkflowNodeType.Collector)]);
        var results = GreedyParentAttribution.Apply(trace, [Pass("c", WorkflowNodeType.Collector)]);

        var result = Assert.Single(results);
        Assert.True(result.Passed);
        Assert.Equal(FailureCategory.None, result.Category);
        Assert.False(result.IsRootCause);
    }
}
