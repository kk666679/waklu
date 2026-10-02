namespace HalalChain.Automation.Browser;

/// <summary>
/// One isolated browser session: its context, page, and lifetime (§10, §11).
///
/// Contract notes:
///   - <see cref="IAsyncDisposable"/> is mandatory, not optional. Every session
///     is a real browser process (or a real WebDriver session) and leaving one
///     running is a resource leak that shows up as an OOM three hours later.
///   - Disposal must be safe to call twice and safe to call after a crash.
///   - Every method takes a CancellationToken: cancellation has to reach the
///     underlying driver, not just the caller's await (§26).
/// </summary>
public interface IBrowserSession : IAsyncDisposable
{
    /// <summary>Opaque id used in logs and telemetry. Not a secret.</summary>
    string SessionId { get; }

    /// <summary>Engine this session is backed by, for attribution.</summary>
    BrowserEngineType EngineType { get; }

    Task NavigateAsync(Uri url, CancellationToken cancellationToken);

    Task<BrowserActionResult> ExecuteAsync(
        BrowserAction action,
        CancellationToken cancellationToken);

    Task<BrowserPageSnapshot> CaptureAsync(CancellationToken cancellationToken);

    /// <summary>Current page URL, for the workflow's outputs.</summary>
    Task<Uri?> GetCurrentUrlAsync(CancellationToken cancellationToken);
}
