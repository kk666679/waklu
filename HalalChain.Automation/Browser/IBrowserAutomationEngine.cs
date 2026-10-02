namespace HalalChain.Automation.Browser;

/// <summary>
/// The browser automation abstraction (§4).
///
/// This is the seam the whole subsystem is built around. The workflow engine
/// holds an <see cref="IBrowserAutomationEngine"/>; it does not know whether
/// Microsoft.Playwright or Selenium.WebDriver is underneath, and it never
/// will — that is the point of the interface existing (ADR-010).
///
/// There is deliberately no member for "run a workflow", "decide", or
/// "record a verdict". The engine can start a browser and do what it is told;
/// control flow and domain decisions stay with the caller (§18, ADR-011 D3).
/// </summary>
public interface IBrowserAutomationEngine
{
    /// <summary>Which engine this implementation is, for logs and telemetry.</summary>
    BrowserEngineType EngineType { get; }

    /// <summary>Human-readable vendor, e.g. "Microsoft.Playwright 1.56".</summary>
    string ImplementationName { get; }

    /// <summary>
    /// Starts an isolated browser context. The caller owns the returned
    /// session and MUST dispose it — including on failure, timeout, and
    /// cancellation (§10). Engines must not pool or share sessions across
    /// executions (§11).
    /// </summary>
    Task<IBrowserSession> CreateSessionAsync(
        BrowserSessionOptions options,
        CancellationToken cancellationToken);
}
