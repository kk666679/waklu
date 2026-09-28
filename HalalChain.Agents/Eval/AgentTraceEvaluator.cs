namespace HalalChain.Agents.Eval;

using System.Text.Json;

using HalalChain.Agents.Eval.Dag;
using HalalChain.Agents.Eval.Dag.Propagation;
using HalalChain.Agents.Eval.Dag.Scorers;
using HalalChain.Agents.Eval.Taxonomy;

/// <summary>
/// Evaluates one trace: score every node, then attribute each failure to its
/// nearest failed ancestor.
///
/// Produces scores, root causes, and failure categories. It does not produce
/// a compliance outcome, and nothing downstream of it may treat its output as
/// one. That separation is the point of the harness: it measures whether the
/// agent pipeline worked, so that tawheed's decision can be trusted to have
/// come from evidence rather than from a broken step.
/// </summary>
public sealed class AgentTraceEvaluator(NodeScorerRegistry scorers)
{
    public async Task<EvalReport> EvaluateAsync(
        AgentTrace trace,
        IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentNullException.ThrowIfNull(goldens);

        var scores = new List<NodeScore>(trace.Nodes.Count);
        foreach (var node in trace.Nodes)
        {
            ct.ThrowIfCancellationRequested();
            var scorer = scorers.GetRequired(node.Type);
            scores.Add(scorer.Score(trace, node, goldens));
        }

        var results = GreedyParentAttribution.Apply(trace, scores);

        return new EvalReport
        {
            TraceId = trace.TraceId,
            WorkflowName = trace.WorkflowName,
            Results = results,
            Taxonomy = GreedyParentAttribution.AggregateTaxonomy(results),
            EvaluatedAt = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>
/// The outcome of evaluating one trace.
///
/// A report is a QA artifact. It describes agent behavior against recorded
/// expectations and carries no authority over compliance status.
/// </summary>
public sealed record EvalReport
{
    public required string TraceId { get; init; }
    public required string WorkflowName { get; init; }
    public required IReadOnlyList<EvalResult> Results { get; init; }
    public required IReadOnlyDictionary<Taxonomy.FailureCategory, int> Taxonomy { get; init; }
    public required DateTimeOffset EvaluatedAt { get; init; }

    public bool Passed => Results.All(r => r.Passed);

    /// <summary>Only the failures something has to be done about.</summary>
    public IReadOnlyList<EvalResult> RootCauseFailures =>
        Results.Where(r => !r.Passed && r.IsRootCause).ToArray();

    /// <summary>
    /// Human-readable failure summary for CI output. Names root causes and
    /// what they propagated to, because "handoff failed" is not actionable and
    /// "collector found 2 of 4 required sources, which propagated to handoff
    /// and gap" is.
    /// </summary>
    public string Describe()
    {
        if (Passed)
            return $"{WorkflowName}/{TraceId}: all {Results.Count} nodes passed.";

        var lines = RootCauseFailures.Select(r =>
        {
            var propagated = Results
                .Where(x => x.PropagatedFrom == r.NodeId)
                .Select(x => x.NodeId)
                .ToArray();

            var suffix = propagated.Length == 0
                ? string.Empty
                : $" (propagated to {string.Join(", ", propagated)})";

            return $"  {r.NodeId} [{r.NodeType}] score={r.Score:F2} " +
                   $"category={r.Category.ToWireName()}{suffix}";
        });

        var rootCount = RootCauseFailures.Count;
        var failedCount = Results.Count(r => !r.Passed);

        return $"{WorkflowName}/{TraceId}: {failedCount} of {Results.Count} nodes failed, " +
               $"{rootCount} root cause(s).\n{string.Join("\n", lines)}";
    }
}

/// <summary>
/// Golden expectations, keyed by node type.
///
/// An absent golden is not a failure. A node whose behavior nobody has
/// recorded an expectation for is simply unmeasured, and the scorers report
/// that distinction explicitly so a report never counts "unmeasured" as
/// "correct".
/// </summary>
public sealed class GoldenExpectationSet
{
    private readonly IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> _goldens;

    public GoldenExpectationSet(IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens)
    {
        ArgumentNullException.ThrowIfNull(goldens);
        _goldens = goldens;
    }

    public static GoldenExpectationSet Empty { get; } =
        new(new Dictionary<WorkflowNodeType, GoldenExpectation>());

    public int Count => _goldens.Count;

    public IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> AsDictionary() => _goldens;

    /// <summary>
    /// Parses the JSONL golden format used by .halalchain/agents/app/eval/goldens/,
    /// so both runtimes read the same expectation files.
    /// </summary>
    public static GoldenExpectationSet ParseJsonl(string jsonl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonl);

        var goldens = new Dictionary<WorkflowNodeType, GoldenExpectation>();
        var lineNumber = 0;

        foreach (var line in jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            lineNumber++;
            if (line.Trim().Length == 0)
                continue;

            JsonElement root;
            try
            {
                root = JsonSerializer.Deserialize<JsonElement>(line);
            }
            catch (JsonException ex)
            {
                throw new FormatException($"Golden line {lineNumber} is not valid JSON: {ex.Message}", ex);
            }

            if (!root.TryGetProperty("node_type", out var nodeTypeElement))
                throw new FormatException($"Golden line {lineNumber} is missing 'node_type'.");

            var nodeType = WorkflowNodeTypes.ParseWireName(nodeTypeElement.GetString());
            var expectations = root.TryGetProperty("expectations", out var e)
                ? e.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal)
                : new Dictionary<string, JsonElement>();

            goldens[nodeType] = new GoldenExpectation
            {
                NodeType = nodeType,
                Expectations = expectations,
            };
        }

        return new GoldenExpectationSet(goldens);
    }
}
