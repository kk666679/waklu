namespace HalalChain.Agents.Eval.Dag.Scorers;

using System.Text.Json;
using HalalChain.Agents.Eval.Taxonomy;

/// <summary>
/// Golden expectations for one node type in one workflow.
///
/// Mirrors one line of a goldens/*.jsonl file. The Python shadow runner reads
/// the same shape, so a golden added there is readable here and vice versa.
/// </summary>
public sealed record GoldenExpectation
{
    public required WorkflowNodeType NodeType { get; init; }
    public IReadOnlyDictionary<string, JsonElement> Expectations { get; init; } =
        new Dictionary<string, JsonElement>();
}

/// <summary>
/// Scores one workflow node against its golden expectation.
///
/// A scorer measures agent behavior. It never interprets that behavior as a
/// compliance outcome, and no scorer may derive one — that is tawheed's role.
/// </summary>
public interface INodeScorer
{
    WorkflowNodeType NodeType { get; }

    NodeScore Score(AgentTrace trace, WorkflowNode node, IReadOnlyDictionary<WorkflowNodeType, GoldenExpectation> goldens);
}

/// <summary>
/// Thresholds are configuration, not scorer internals — DECISION-CONTRACT
/// invariant 4. A scorer reports its score; whether that score passes is
/// decided against these values, so tightening a gate is a config change.
/// </summary>
public sealed record NodeThresholds
{
    public double Collector { get; init; } = 0.8;
    public double Classifier { get; init; } = 0.9;
    public double Verifier { get; init; } = 1.0;
    public double Handoff { get; init; } = 0.7;
    public double Gap { get; init; } = 0.7;
    public double Recollection { get; init; } = 1.0;
    public double Terminal { get; init; } = 1.0;

    /// <summary>Threshold for a node type, falling back to 0.7 for unknown types.</summary>
    public double For(WorkflowNodeType type) => type switch
    {
        WorkflowNodeType.Collector => Collector,
        WorkflowNodeType.Classifier => Classifier,
        WorkflowNodeType.Verifier => Verifier,
        WorkflowNodeType.Handoff => Handoff,
        WorkflowNodeType.Gap => Gap,
        WorkflowNodeType.Recollection => Recollection,
        WorkflowNodeType.Terminal => Terminal,
        _ => 0.7,
    };
}

public static class NodeOutput
{
    /// <summary>Reads a string list from a node's output payload.</summary>
    public static IReadOnlyList<string> StringList(IReadOnlyDictionary<string, object?> output, string key)
    {
        if (!output.TryGetValue(key, out var raw) || raw is null)
            return Array.Empty<string>();

        return raw switch
        {
            IEnumerable<string> strings => strings.ToArray(),
            JsonElement { ValueKind: JsonValueKind.Array } element => element
                .EnumerateArray()
                .Select(e => e.ToString())
                .ToArray(),
            _ => new[] { raw.ToString() ?? string.Empty },
        };
    }

    public static string? String(IReadOnlyDictionary<string, object?> output, string key)
    {
        if (!output.TryGetValue(key, out var raw) || raw is null)
            return null;

        return raw switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => raw.ToString(),
        };
    }

    public static bool Bool(IReadOnlyDictionary<string, object?> output, string key, bool fallback = false)
    {
        if (!output.TryGetValue(key, out var raw) || raw is null)
            return fallback;

        return raw switch
        {
            bool b => b,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            _ => fallback,
        };
    }

    /// <summary>Reads a nested object as a dictionary, for bundle-shaped outputs.</summary>
    public static IReadOnlyDictionary<string, JsonElement> Object(
        IReadOnlyDictionary<string, object?> output, string key)
    {
        if (!output.TryGetValue(key, out var raw) || raw is null)
            return new Dictionary<string, JsonElement>();

        if (raw is IReadOnlyDictionary<string, JsonElement> typed)
            return typed;

        if (raw is JsonElement { ValueKind: JsonValueKind.Object } element)
            return element.EnumerateObject().ToDictionary(
                p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal);

        return new Dictionary<string, JsonElement>();
    }
}
