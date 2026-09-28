namespace HalalChain.Agents.Eval.Dag.Scorers;

using System.Text.Json;
using HalalChain.Agents.Eval.Taxonomy;

/// <summary>
/// Scores the handoff node: does the evidence bundle satisfy tawheed's input
/// contract before tawheed is called?
///
/// This is the most important scorer in the set, because the handoff is where
/// an agent's output becomes tawheed's input. A bundle missing a required
/// field does not merely score badly — it produces a tawheed call that
/// cannot be evaluated, and the failure is silent if nobody checks.
///
/// The Python original delegates quality scoring to a DeepEval LLM rubric.
/// This implementation is structural only: it checks that every required
/// field is present and non-empty. That is deliberate. An LLM grading the
/// completeness of the bundle it is about to grade would be a second,
/// unauditable opinion sitting in the trust path. Field presence is checkable;
/// field quality is tawheed's assessment, and belongs to tawheed.
/// </summary>
public sealed class HandoffScorer : INodeScorer
{
    /// <summary>
    /// The fields tawheed requires to evaluate a case. Mirrors REQUIRED_FIELDS
    /// in .halalchain/agents/app/eval/metrics/handoff_contract.py. If tawheed's
    /// contract changes, this list changes with it.
    /// </summary>
    public static readonly IReadOnlyList<string> RequiredFields =
        ["product_id", "certificate", "ingredients", "manufacturer"];

    public WorkflowNodeType NodeType => WorkflowNodeType.Handoff;

    public NodeScore Score(
        AgentTrace trace,
        WorkflowNode node,
        IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens)
    {
        var bundle = node.Output.TryGetValue("evidence_bundle", out var raw) && raw is JsonElement element
            ? ToDictionary(element)
            : ReadDictionary(raw);

        var missing = RequiredFields
            .Where(f => !bundle.TryGetValue(f, out var v) || IsEmpty(v))
            .ToArray();

        var passed = missing.Length == 0;
        var score = passed ? 1.0 : 0.0;

        return new NodeScore
        {
            NodeId = node.Id,
            NodeType = WorkflowNodeType.Handoff,
            Score = score,
            Passed = passed,
            Category = passed
                ? FailureCategory.None
                : FailureCategory.HandoffMissingRequiredField,
            Details = CollectorScorer.Details(
                ("missing_required_fields", missing),
                ("present_fields", bundle.Keys.ToArray()),
                ("reason", passed ? "contract_satisfied" : "missing_required_field")),
        };
    }

    private static bool IsEmpty(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => true,
        JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()),
        JsonValueKind.Array => value.GetArrayLength() == 0,
        JsonValueKind.Object => !value.EnumerateObject().Any(),
        JsonValueKind.False => true,
        _ => false,
    };

    private static IReadOnlyDictionary<string, JsonElement> ToDictionary(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal)
            : new Dictionary<string, JsonElement>();

    private static IReadOnlyDictionary<string, JsonElement> ReadDictionary(object? raw)
    {
        if (raw is IReadOnlyDictionary<string, JsonElement> typed)
            return typed;

        if (raw is IDictionary<string, object?> loose)
            return loose.ToDictionary(
                kv => kv.Key,
                kv => JsonSerializer.SerializeToElement(kv.Value),
                StringComparer.Ordinal);

        return new Dictionary<string, JsonElement>();
    }
}

/// <summary>
/// Scores the terminal node: does the workflow's terminal outcome match the
/// human label recorded in the golden?
///
/// Read this carefully, because the name is close to the one thing this
/// project forbids. This scorer compares two values that already exist — the
/// outcome tawheed produced, and the outcome a human reviewer recorded. It
/// does not produce either one, and it cannot: it has no policy knowledge and
/// no authority to decide. It measures whether the agent pipeline faithfully
/// carried tawheed's decision to the end, which is a question about plumbing,
/// not about compliance.
///
/// The scored value is a comparison outcome, deliberately not named
/// "verdict", and the result is a score. A workflow that routed around
/// tawheed and answered this correctly on its own would still be a defect —
/// one this scorer exists to catch, not to reward.
/// </summary>
public sealed class TerminalScorer(NodeThresholds thresholds) : INodeScorer
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Terminal;

    public NodeScore Score(
        AgentTrace trace,
        WorkflowNode node,
        IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens)
    {
        var expectations = CollectorScorer.Expectations(goldens, WorkflowNodeType.Terminal);
        var expected = expectations.TryGetValue("expected_outcome", out var outcome)
            ? outcome
            : expectations.GetValueOrDefault("expected_verdict");

        var actual = NodeOutput.String(node.Output, "outcome")
            ?? NodeOutput.String(node.Output, "verdict");

        if (expected.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return new NodeScore
            {
                NodeId = node.Id,
                NodeType = WorkflowNodeType.Terminal,
                Score = 1.0,
                Passed = true,
                Details = CollectorScorer.Details(("note", "no human label recorded for terminal node")),
            };
        }

        var matches = string.Equals(expected.ToString(), actual, StringComparison.OrdinalIgnoreCase);
        var score = matches ? 1.0 : 0.0;
        var passed = score >= thresholds.Terminal;

        return new NodeScore
        {
            NodeId = node.Id,
            NodeType = WorkflowNodeType.Terminal,
            Score = score,
            Passed = passed,
            Category = passed ? FailureCategory.None : FailureCategory.TerminalOutcomeMismatch,
            Details = CollectorScorer.Details(
                ("expected", expected.ToString()),
                ("actual", actual),
                ("note", "compares against a human label; produces no outcome")),
        };
    }
}
