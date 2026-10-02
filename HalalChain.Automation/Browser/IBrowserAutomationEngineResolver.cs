namespace HalalChain.Automation.Browser;

/// <summary>
/// Maps an engine request to an implementation (§7).
///
/// Workflow definitions reference <see cref="BrowserEngineType"/>, never a
/// concrete engine class. This resolver is the single place a name becomes an
/// object, so adding a third engine later is one registration — no workflow
/// and no node changes (§42).
/// </summary>
public interface IBrowserAutomationEngineResolver
{
    /// <summary>
    /// Resolves an engine. <see cref="BrowserEngineType.Default"/> maps to the
    /// configured default; an engine with no registration throws
    /// <see cref="BrowserEngineException"/> rather than silently substituting
    /// another, because a silent substitution makes "we ran it on Selenium"
    /// a lie in the audit trail.
    /// </summary>
    IBrowserAutomationEngine Resolve(BrowserEngineType engineType);

    /// <summary>The engine <see cref="BrowserEngineType.Default"/> resolves to.</summary>
    BrowserEngineType ConfiguredDefault { get; }

    /// <summary>Engines currently registered, for health checks and diagnostics.</summary>
    IReadOnlyCollection<BrowserEngineType> AvailableEngines { get; }
}

/// <summary>
/// Raised when an engine cannot be resolved. Distinct from a browser-level
/// failure so an operator can tell "misconfigured" from "the page broke".
/// </summary>
public sealed class BrowserEngineException : Exception
{
    public BrowserEngineException(BrowserEngineType requested, string message)
        : base(message) => Requested = requested;

    public BrowserEngineType Requested { get; }
}
