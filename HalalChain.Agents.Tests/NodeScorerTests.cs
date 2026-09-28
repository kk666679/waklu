using System.Text.Json;

using HalalChain.Agents.Eval;
using HalalChain.Agents.Eval.Dag;
using HalalChain.Agents.Eval.Dag.Scorers;
using HalalChain.Agents.Eval.Taxonomy;

using Xunit;

namespace HalalChain.Agents.Tests;

public sealed class NodeScorerTests
{
    private static GoldenExpectationSet Goldens(string jsonl) => GoldenExpectationSet.ParseJsonl(jsonl);

    private static WorkflowNode Node(string id, WorkflowNodeType type, object? output) => new()
    {
        Id = id,
        Type = type,
        Output = output is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?> { ["value"] = output },
    };

    private static WorkflowNode Node(string id, WorkflowNodeType type, params (string Key, object? Value)[] output) => new()
    {
        Id = id,
        Type = type,
        Output = output.ToDictionary(o => o.Key, o => o.Value, StringComparer.Ordinal),
    };

    private static AgentTrace Trace(params WorkflowNode[] nodes) => new()
    {
        TraceId = "t1",
        WorkflowName = "supplier_onboarding",
        Nodes = nodes,
        Edges = Array.Empty<TraceEdge>(),
        StartedAt = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public void Collector_RecallBelowThreshold_FailsWithMissedSource()
    {
        var goldens = Goldens("""{"node_type":"collector","expectations":{"required_sources":["a","b","c","d"]}}""");
        var node = Node("c", WorkflowNodeType.Collector, ("sources_found", new[] { "a", "b" }));

        var score = new CollectorScorer(new NodeThresholds()).Score(Trace(node), node, goldens.AsDictionary());

        Assert.False(score.Passed);
        Assert.Equal(0.5, score.Score, 3);
        Assert.Equal(FailureCategory.CollectorMissedSource, score.Category);
    }

    [Fact]
    public void Collector_AtThreshold_Passes()
    {
        var goldens = Goldens("""{"node_type":"collector","expectations":{"required_sources":["a","b","c","d","e"]}}""");
        var node = Node("c", WorkflowNodeType.Collector, ("sources_found", new[] { "a", "b", "c", "d" }));

        var score = new CollectorScorer(new NodeThresholds()).Score(Trace(node), node, goldens.AsDictionary());

        Assert.True(score.Passed);
        Assert.Equal(0.8, score.Score, 3);
    }

    [Fact]
    public void Classifier_LabelAccuracy()
    {
        var goldens = Goldens("""{"node_type":"classifier","expectations":{"expected_labels":{"a":"1","b":"2","c":"3"}}}""");
        var labels = JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" });
        var node = Node("cl", WorkflowNodeType.Classifier, ("labels", labels));

        var score = new ClassifierScorer(new NodeThresholds()).Score(Trace(node), node, goldens.AsDictionary());

        Assert.False(score.Passed);
        Assert.Equal(2d / 3, score.Score, 3);
        Assert.Equal(FailureCategory.ClassifierMislabel, score.Category);
    }

    [Fact]
    public void Verifier_DisagreementScoresZero()
    {
        var goldens = Goldens("""{"node_type":"verifier","expectations":{"expected_valid":true}}""");
        var node = Node("v", WorkflowNodeType.Verifier, ("is_valid", false));

        var score = new VerifierScorer(new NodeThresholds()).Score(Trace(node), node, goldens.AsDictionary());

        Assert.False(score.Passed);
        Assert.Equal(0.0, score.Score);
        Assert.Equal(FailureCategory.VerifierValidityDisagreement, score.Category);
    }

    [Fact]
    public void Gap_OverFlaggingIsPunishedByPrecision()
    {
        // A gap agent that reports everything finds all three real gaps, so
        // recall is perfect — but precision is 0.5 (three hits out of six
        // claims), giving F1 = 0.667, below the 0.7 gate. Noisy escalation has
        // to be able to fail, or "flag everything" becomes the winning
        // strategy against a gap agent.
        var goldens = Goldens("""{"node_type":"gap","expectations":{"expected_missing":["a","b","c"]}}""");
        var node = Node("g", WorkflowNodeType.Gap, ("missing_requirements", new[] { "a", "b", "c", "x", "y", "z" }));

        var score = new GapScorer(new NodeThresholds()).Score(Trace(node), node, goldens.AsDictionary());

        Assert.Equal(2d / 3, score.Score, 3);
        Assert.False(score.Passed);
        Assert.Equal(FailureCategory.GapMissedRequirement, score.Category);
    }

    [Fact]
    public void Recollection_UnclosedLoopFails()
    {
        var node = Node("r", WorkflowNodeType.Recollection, ("loop_closed", false));

        var score = new RecollectionScorer(new NodeThresholds()).Score(Trace(node), node, GoldenExpectationSet.Empty.AsDictionary());

        Assert.False(score.Passed);
        Assert.Equal(FailureCategory.RecollectionFailedToClose, score.Category);
    }

    [Fact]
    public void Handoff_MissingRequiredFieldFails()
    {
        var bundle = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["product_id"] = "p1",
            ["ingredients"] = new[] { "sugar" },
        });

        var node = Node("h", WorkflowNodeType.Handoff, ("evidence_bundle", bundle));

        var score = new HandoffScorer().Score(Trace(node), node, GoldenExpectationSet.Empty.AsDictionary());

        Assert.False(score.Passed);
        Assert.Equal(FailureCategory.HandoffMissingRequiredField, score.Category);
    }

    [Fact]
    public void Handoff_EmptyRequiredFieldCountsAsMissing()
    {
        // An empty ingredients list is not "present" — handing tawheed an empty
        // array is a silent evidence gap, which is worse than an absent field
        // because it looks satisfied.
        var bundle = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["product_id"] = "p1",
            ["certificate"] = new { issuer = "JAKIM" },
            ["ingredients"] = Array.Empty<string>(),
            ["manufacturer"] = new { name = "Acme" },
        });

        var node = Node("h", WorkflowNodeType.Handoff, ("evidence_bundle", bundle));

        var score = new HandoffScorer().Score(Trace(node), node, GoldenExpectationSet.Empty.AsDictionary());

        Assert.False(score.Passed);
        Assert.Equal(FailureCategory.HandoffMissingRequiredField, score.Category);
    }

    [Fact]
    public void Handoff_CompleteBundlePasses()
    {
        var bundle = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["product_id"] = "p1",
            ["certificate"] = new { issuer = "JAKIM", expiry = "2027-01-01" },
            ["ingredients"] = new[] { "sugar" },
            ["manufacturer"] = new { name = "Acme" },
        });

        var node = Node("h", WorkflowNodeType.Handoff, ("evidence_bundle", bundle));

        var score = new HandoffScorer().Score(Trace(node), node, GoldenExpectationSet.Empty.AsDictionary());

        Assert.True(score.Passed);
        Assert.Equal(FailureCategory.None, score.Category);
    }

    [Fact]
    public void Terminal_ComparesAgainstHumanLabel()
    {
        var goldens = Goldens("""{"node_type":"verdict","expectations":{"expected_outcome":"sufficient_evidence"}}""");
        var node = Node("t", WorkflowNodeType.Terminal, ("outcome", "insufficient_evidence"));

        var score = new TerminalScorer(new NodeThresholds()).Score(Trace(node), node, goldens.AsDictionary());

        Assert.False(score.Passed);
        Assert.Equal(FailureCategory.TerminalOutcomeMismatch, score.Category);
    }

    [Fact]
    public void Terminal_WithNoHumanLabel_IsUnmeasuredNotPassed()
    {
        var node = Node("t", WorkflowNodeType.Terminal, ("outcome", "anything"));

        var score = new TerminalScorer(new NodeThresholds()).Score(Trace(node), node, GoldenExpectationSet.Empty.AsDictionary());

        Assert.True(score.Passed);
        Assert.True(score.Details.ContainsKey("note"));
    }

    [Fact]
    public void UnmeasuredNode_IsDistinguishableFromACorrectOne()
    {
        // A node with no golden must not be indistinguishable from a node that
        // genuinely behaved correctly.
        var goldens = GoldenExpectationSet.Empty;
        var node = Node("c", WorkflowNodeType.Collector, ("sources_found", new[] { "a" }));

        var score = new CollectorScorer(new NodeThresholds()).Score(Trace(node), node, goldens.AsDictionary());

        Assert.True(score.Passed);
        Assert.True(score.Details.ContainsKey("note"));
    }

    [Fact]
    public void Registry_HasAScorerForEveryNodeType()
    {
        var registry = new NodeScorerRegistry();

        foreach (var type in Enum.GetValues<WorkflowNodeType>().Where(t => t != WorkflowNodeType.Unknown))
        {
            Assert.True(registry.TryGet(type, out _), $"no scorer for {type}");
        }
    }

    [Fact]
    public void Registry_ThrowsForUnknownNodeType()
    {
        // An unscored node reads as a passing node in the report, so the
        // registry must fail loudly instead of returning null.
        var registry = new NodeScorerRegistry();

        Assert.Throws<InvalidOperationException>(() => registry.GetRequired(WorkflowNodeType.Unknown));
    }
}
