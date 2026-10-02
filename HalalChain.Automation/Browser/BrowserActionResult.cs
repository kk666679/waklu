namespace HalalChain.Automation.Browser;

using System.Text.Json.Serialization;

/// <summary>
/// Result of executing one <see cref="BrowserAction"/>.
///
/// Mirrors the shape the workflow runner and the designer consume:
/// a status, the produced outputs, and — for captures — the evidence
/// references. Never carries a raw vendor exception to the caller; the
/// adapters normalise first (§28) so the UI shows
/// "Element not found: role=button (Submit)" rather than a Playwright stack.
/// </summary>
public sealed record BrowserActionResult
{
    public required BrowserActionStatus Status { get; init; }

    public IReadOnlyDictionary<string, object?> Outputs { get; init; }
        = new Dictionary<string, object?>();

    public AutomationErrorKind? ErrorKind { get; init; }

    public string? ErrorMessage { get; init; }

    /// <summary>Evidence ids produced by screenshot / html / capture actions.</summary>
    public IReadOnlyList<string> EvidenceIds { get; init; } = [];

    public Uri? CurrentUrl { get; init; }

    public static BrowserActionResult Success(
        IReadOnlyDictionary<string, object?>? outputs = null,
        Uri? currentUrl = null,
        IReadOnlyList<string>? evidenceIds = null)
        => new()
        {
            Status = BrowserActionStatus.Succeeded,
            Outputs = outputs ?? new Dictionary<string, object?>(),
            CurrentUrl = currentUrl,
            EvidenceIds = evidenceIds ?? [],
        };

    public static BrowserActionResult Fail(
        AutomationErrorKind kind,
        string message,
        Uri? currentUrl = null)
        => new()
        {
            Status = BrowserActionStatus.Failed,
            ErrorKind = kind,
            ErrorMessage = message,
            CurrentUrl = currentUrl,
        };

    public static BrowserActionResult Cancelled(string? message = null) => new()
    {
        Status = BrowserActionStatus.Cancelled,
        ErrorKind = AutomationErrorKind.ExecutionCancelled,
        ErrorMessage = message,
    };
}

public enum BrowserActionStatus
{
    Succeeded,
    Failed,
    Cancelled,
    Skipped,
}
