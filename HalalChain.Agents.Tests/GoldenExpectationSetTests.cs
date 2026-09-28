using System.Text.Json;

using HalalChain.Agents.Eval;
using HalalChain.Agents.Eval.Dag;
using HalalChain.Agents.Eval.Dag.Scorers;
using HalalChain.Application.Agentic.Models;
using HalalChain.Agents.Eval.Taxonomy;
using HalalChain.Agents.Runtime;

using Xunit;

namespace HalalChain.Agents.Tests;

public sealed class GoldenExpectationSetTests
{
    [Fact]
    public void ParsesTheJsonlFormatThePythonRunnerUses()
    {
        const string jsonl = """
            {"node_type": "collector", "expectations": {"required_sources": ["a", "b"]}}
            {"node_type": "classifier", "expectations": {"expected_labels": {"certificate": "valid"}}}
            """;

        var goldens = GoldenExpectationSet.ParseJsonl(jsonl);

        Assert.Equal(2, goldens.Count);
        Assert.True(goldens.AsDictionary().ContainsKey(WorkflowNodeType.Collector));
        Assert.True(goldens.AsDictionary().ContainsKey(WorkflowNodeType.Classifier));
    }

    [Fact]
    public void RealSupplierOnboardingGoldens_Parse()
    {
        // The actual file committed at
        // .halalchain/agents/app/eval/goldens/supplier_onboarding.jsonl
        const string jsonl = """
            {"node_type": "collector", "expectations": {"required_sources": ["certificate_db", "supplier_portal", "lab_results"]}}
            {"node_type": "classifier", "expectations": {"expected_labels": {"certificate": "valid", "ingredients": "halal", "manufacturer": "verified"}}}
            {"node_type": "verifier", "expectations": {"expected_valid": true}}
            {"node_type": "handoff", "expectations": {"expected_bundle": {"product_id": "string", "certificate": "object", "ingredients": "array", "manufacturer": "object"}}}
            {"node_type": "gap", "expectations": {"expected_missing": ["lab_certificate", "traceability_doc"]}}
            """;

        var goldens = GoldenExpectationSet.ParseJsonl(jsonl);

        Assert.Equal(5, goldens.Count);
    }

    [Fact]
    public void RealCertificateReviewGoldens_Parse()
    {
        const string jsonl = """
            {"node_type": "collector", "expectations": {"required_sources": ["certificate_issuer_api", "expiry_registry"]}}
            {"node_type": "classifier", "expectations": {"expected_labels": {"certificate": "valid", "standard": "MS1500:2019", "scope": "product"}}}
            {"node_type": "verifier", "expectations": {"expected_valid": true}}
            {"node_type": "handoff", "expectations": {"expected_bundle": {"certificate_id": "string", "issuer": "string", "expiry": "date", "standard": "string"}}}
            {"node_type": "gap", "expectations": {"expected_missing": ["renewal_proof"]}}
            """;

        Assert.Equal(5, GoldenExpectationSet.ParseJsonl(jsonl).Count);
    }

    [Fact]
    public void LineWithoutNodeTypeIsAFormatError()
    {
        Assert.Throws<FormatException>(() =>
            GoldenExpectationSet.ParseJsonl("""{"expectations": {}}"""));
    }

    [Fact]
    public void MalformedJsonIsAFormatErrorWithTheLineNumber()
    {
        var ex = Assert.Throws<FormatException>(() =>
            GoldenExpectationSet.ParseJsonl("""
                {"node_type": "collector", "expectations": {}}
                not json
                """));

        Assert.Contains("line 2", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownNodeTypeFailsRatherThanBeingSkipped()
    {
        Assert.Throws<ArgumentException>(() =>
            GoldenExpectationSet.ParseJsonl("""{"node_type": "telepathy", "expectations": {}}"""));
    }

    [Fact]
    public void TerminalNodeTypeMapsToThePythonVerdictWireName()
    {
        Assert.Equal("verdict", WorkflowNodeType.Terminal.ToWireName());
    }
}

public sealed class WorkflowBudgetTests
{
    [Fact]
    public void BudgetRun_ReportsRemainingSteps()
    {
        var ledger = new WorkflowBudgetLedger();
        using var run = ledger.Begin(new WorkflowBudget(MaxSteps: 3));

        Assert.Equal(3, run.StepsRemaining);
        Assert.False(run.IsExhausted);
    }

    [Fact]
    public void BudgetRun_IsExhaustedOnceMaxWallSecondsIsZero()
    {
        var ledger = new WorkflowBudgetLedger();
        using var run = ledger.Begin(new WorkflowBudget(MaxWallSeconds: 0));

        Assert.True(run.IsExhausted);
    }

    [Fact]
    public void EveryRunGetsADistinctId()
    {
        // Run ids are what an audit record cites, so two runs sharing one id
        // would make provenance ambiguous.
        var ledger = new WorkflowBudgetLedger();

        using var a = ledger.Begin(new WorkflowBudget());
        using var b = ledger.Begin(new WorkflowBudget());

        Assert.NotEqual(a.Id, b.Id);
    }
}

public sealed class AgentTraceEvaluatorTests
{
    private static AgentTrace Trace(params WorkflowNode[] nodes) => new()
    {
        TraceId = "t1",
        WorkflowName = "supplier_onboarding",
        Nodes = nodes,
        Edges = Array.Empty<TraceEdge>(),
        StartedAt = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public async Task ReportCollapsesACascadeIntoOneRootCause()
    {
        var collector = new WorkflowNode
        {
            Id = "collector",
            Type = WorkflowNodeType.Collector,
            Output = new Dictionary<string, object?> { ["sources_found"] = new[] { "a" } },
        };
        var handoff = new WorkflowNode { Id = "handoff", Type = WorkflowNodeType.Handoff };

        var trace = Trace(collector, handoff);
        trace = trace with { Edges = [new TraceEdge("collector", "handoff")] };

        var goldens = GoldenExpectationSet.ParseJsonl("""
            {"node_type":"collector","expectations":{"required_sources":["a","b","c"]}}
            """);

        var report = await new AgentTraceEvaluator(new NodeScorerRegistry())
            .EvaluateAsync(trace, goldens.AsDictionary(), TestContext.Current.CancellationToken);

        Assert.False(report.Passed);
        Assert.Single(report.RootCauseFailures);
        Assert.Equal("collector", report.RootCauseFailures[0].NodeId);
    }

    [Fact]
    public async Task DescribeNamesTheRootCauseAndWhatItPropagatedTo()
    {
        var trace = Trace(
            new WorkflowNode
            {
                Id = "collector",
                Type = WorkflowNodeType.Collector,
                Output = new Dictionary<string, object?> { ["sources_found"] = new[] { "a" } },
            },
            new WorkflowNode { Id = "handoff", Type = WorkflowNodeType.Handoff })
        with { Edges = [new TraceEdge("collector", "handoff")] };

        var goldens = GoldenExpectationSet.ParseJsonl("""
            {"node_type":"collector","expectations":{"required_sources":["a","b","c"]}}
            """);

        var report = await new AgentTraceEvaluator(new NodeScorerRegistry())
            .EvaluateAsync(trace, goldens.AsDictionary(), TestContext.Current.CancellationToken);

        var description = report.Describe();

        Assert.Contains("collector", description);
        Assert.Contains("propagated to handoff", description);
        Assert.Contains("collector.missed_source", description);
    }

    [Fact]
    public async Task CleanTraceReportsPass()
    {
        var trace = Trace(new WorkflowNode
        {
            Id = "collector",
            Type = WorkflowNodeType.Collector,
            Output = new Dictionary<string, object?> { ["sources_found"] = new[] { "a", "b", "c" } },
        });

        var goldens = GoldenExpectationSet.ParseJsonl("""
            {"node_type":"collector","expectations":{"required_sources":["a","b","c"]}}
            """);

        var report = await new AgentTraceEvaluator(new NodeScorerRegistry())
            .EvaluateAsync(trace, goldens.AsDictionary(), TestContext.Current.CancellationToken);

        Assert.True(report.Passed);
        Assert.Empty(report.RootCauseFailures);
    }
}
