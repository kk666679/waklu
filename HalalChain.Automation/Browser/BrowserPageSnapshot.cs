namespace HalalChain.Automation.Browser;

using System.Text.Json.Serialization;

/// <summary>
/// Normalised automation errors.
///
/// The point is that a Playwright timeout and a Selenium timeout are the same
/// thing to the workflow engine, the retry policy, and the operator. Adapters
/// map vendor exceptions onto these kinds at the seam (§28); anything that
/// leaks a raw <c>PlaywrightException</c> to a workflow author is a defect.
/// </summary>
public enum AutomationErrorKind
{
    /// <summary>Unexpected — a bug or an unclassified vendor failure.</summary>
    Unknown = 0,

    BrowserLaunchFailed,
    NavigationFailed,
    ElementNotFound,
    ElementNotInteractable,
    Timeout,
    AuthenticationFailed,
    DownloadFailed,
    UploadFailed,
    WorkflowValidationFailed,
    CredentialUnavailable,
    IntegrationFailed,
    ExecutionCancelled,

    /// <summary>Navigation blocked by the egress allowlist (ADR-011 D1).</summary>
    NavigationBlocked,

    /// <summary>The resolved engine has no implementation registered.</summary>
    EngineUnavailable,
}

/// <summary>
/// A page as captured at one instant — enough to audit what a node saw without
/// retaining the live browser (ADR-011 D3).
/// </summary>
public sealed record BrowserPageSnapshot
{
    public required Uri? Url { get; init; }

    public required string Title { get; init; }

    public string? Html { get; init; }

    public byte[]? ScreenshotPng { get; init; }

    public IReadOnlyList<string> ConsoleMessages { get; init; } = [];

    public IReadOnlyList<BrowserNetworkEntry> Network { get; init; } = [];

    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// One request/response observed while the page ran. Deliberately carries no
/// request bodies or cookies — those are credential-adjacent and must not be
/// persisted with evidence (ADR-011 D2).
/// </summary>
public sealed record BrowserNetworkEntry(
    string Method,
    Uri Url,
    int? Status,
    double DurationMs);
