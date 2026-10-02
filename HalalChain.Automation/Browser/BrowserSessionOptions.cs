namespace HalalChain.Automation.Browser;

/// <summary>
/// Per-session, per-execution settings (§11).
///
/// There is no global/static session state anywhere in the subsystem: these
/// options are created per run, which is what makes "two tenants never share
/// cookies" a structural property rather than a discipline (ADR-011 D4).
/// </summary>
public sealed record BrowserSessionOptions
{
    /// <summary>Which engine to use. Resolved by <see cref="IBrowserAutomationEngineResolver"/>.</summary>
    public BrowserEngineType Engine { get; init; } = BrowserEngineType.Default;

    public bool Headless { get; init; } = true;

    public TimeSpan ActionTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan NavigationTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Hosts this session may navigate to. Empty denies everything. Always
    /// copied from <see cref="BrowserOptions"/> at session creation — a caller
    /// cannot widen its own allowlist at runtime.
    /// </summary>
    public IReadOnlyList<string> AllowedHosts { get; init; } = [];

    /// <summary>Server-assigned tenant. Required — sessions are tenant-scoped.</summary>
    public required string TenantId { get; init; }

    /// <summary>Correlates session lifecycle events with the triggering job.</summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    public int MaxNavigations { get; init; } = 50;

    public int MaxActions { get; init; } = 500;

    /// <summary>Artefact directory root for engines that must touch disk.</summary>
    public string? ArtifactDirectory { get; init; }

    /// <summary>Optional browser flavour override (Chromium/Firefox/WebKit).</summary>
    public BrowserFlavor? Flavor { get; init; }
}

/// <summary>
/// Browser families both engines can address. Kept abstract so a workflow never
/// names a vendor-specific binary.
/// </summary>
public enum BrowserFlavor
{
    Chromium,
    Firefox,
    WebKit,
}
