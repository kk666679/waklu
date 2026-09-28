namespace HalalChain.Agents.Eval.Dag;

/// <summary>
/// Node roles in an agent workflow DAG. The type of a node determines which
/// scorer evaluates it, so adding a node type without adding a scorer is a
/// build-visible gap rather than a silently-unscored node.
/// </summary>
public enum WorkflowNodeType
{
    Unknown = 0,
    Collector,
    Classifier,
    Verifier,
    Handoff,
    Gap,
    Recollection,
    Terminal,
}

public static class WorkflowNodeTypes
{
    private static readonly IReadOnlyDictionary<WorkflowNodeType, string> WireNames =
        new Dictionary<WorkflowNodeType, string>
        {
            [WorkflowNodeType.Unknown] = "unknown",
            [WorkflowNodeType.Collector] = "collector",
            [WorkflowNodeType.Classifier] = "classifier",
            [WorkflowNodeType.Verifier] = "verifier",
            [WorkflowNodeType.Handoff] = "handoff",
            [WorkflowNodeType.Gap] = "gap",
            [WorkflowNodeType.Recollection] = "recollection",
            [WorkflowNodeType.Terminal] = "verdict",
        };

    public static string ToWireName(this WorkflowNodeType type) => WireNames[type];

    /// <summary>
    /// Parses a wire name. <c>null</c> maps to <see cref="WorkflowNodeType.Unknown"/>;
    /// an unrecognized non-empty name throws, because a node type with no scorer
    /// is a gap that must be closed rather than a node to skip.
    /// </summary>
    public static WorkflowNodeType ParseWireName(string? wireName)
    {
        if (string.IsNullOrEmpty(wireName))
            return WorkflowNodeType.Unknown;

        foreach (var (type, name) in WireNames)
            if (string.Equals(name, wireName, StringComparison.Ordinal))
                return type;

        throw new ArgumentException(
            $"Unknown workflow node type '{wireName}'. Add it to WorkflowNodeTypes and " +
            "register a scorer for it, or evaluation will skip the node silently.",
            nameof(wireName));
    }
}

/// <summary>One node's output payload as the scorer sees it.</summary>
public sealed record WorkflowNode
{
    public required string Id { get; init; }
    public required WorkflowNodeType Type { get; init; }
    public IReadOnlyDictionary<string, object?> Output { get; init; } =
        new Dictionary<string, object?>();
}

/// <summary>
/// A normalized agent execution trace.
///
/// This is evaluation input, not a decision. It records what the agents did;
/// whether that was correct is the scorer's problem, and what the result
/// *means* is tawheed's.
/// </summary>
public sealed record AgentTrace
{
    public required string TraceId { get; init; }
    public required string WorkflowName { get; init; }
    public required IReadOnlyList<WorkflowNode> Nodes { get; init; }
    public required IReadOnlyList<TraceEdge> Edges { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string Status { get; init; } = "success";
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}

public sealed record TraceEdge(string FromNodeId, string ToNodeId);
