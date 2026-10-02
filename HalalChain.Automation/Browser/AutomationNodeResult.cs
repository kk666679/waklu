namespace HalalChain.Automation.Browser;

using System.Text.Json.Serialization;

/// <summary>
/// The structured result of running one workflow node (§20).
///
/// Every node — browser or not — produces this. The workflow engine decides
/// control flow from it; the browser engine never does (§18). Keeping the
/// shape uniform means the designer renders success/failure the same way for
/// an HTTP node and a browser node, and means a future non-browser node type
/// needs no change to the runner.
/// </summary>
public sealed record AutomationNodeResult
{
    public required string NodeId { get; init; }

    public required AutomationNodeStatus Status { get; init; }

    public IReadOnlyDictionary<string, object?> Outputs { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);

    public AutomationErrorKind? ErrorKind { get; init; }

    public string? Error { get; init; }

    public TimeSpan Duration { get; init; }

    public IReadOnlyList<string> EvidenceIds { get; init; } = [];

    public static AutomationNodeResult Succeeded(
        string nodeId,
        IReadOnlyDictionary<string, object?>? outputs = null,
        TimeSpan duration = default,
        IReadOnlyList<string>? evidenceIds = null)
        => new()
        {
            NodeId = nodeId,
            Status = AutomationNodeStatus.Succeeded,
            Outputs = outputs ?? new Dictionary<string, object?>(StringComparer.Ordinal),
            Duration = duration,
            EvidenceIds = evidenceIds ?? [],
        };

    public static AutomationNodeResult Failed(
        string nodeId,
        AutomationErrorKind kind,
        string message,
        TimeSpan duration = default)
        => new()
        {
            NodeId = nodeId,
            Status = AutomationNodeStatus.Failed,
            ErrorKind = kind,
            Error = message,
            Duration = duration,
        };

    public static AutomationNodeResult Cancelled(string nodeId, string? message = null)
        => new()
        {
            NodeId = nodeId,
            Status = AutomationNodeStatus.Cancelled,
            ErrorKind = AutomationErrorKind.ExecutionCancelled,
            Error = message,
        };

    public static AutomationNodeResult Skipped(string nodeId, string reason)
        => new()
        {
            NodeId = nodeId,
            Status = AutomationNodeStatus.Skipped,
            Error = reason,
        };
}

public enum AutomationNodeStatus
{
    Succeeded,
    Failed,
    Cancelled,
    Skipped,
}
