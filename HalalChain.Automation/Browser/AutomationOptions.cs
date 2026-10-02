namespace HalalChain.Automation.Browser;

using Microsoft.Extensions.Configuration;

/// <summary>
/// The single configuration surface for browser automation (§34).
///
/// Every value the subsystem reads lives here, so an operator can see the whole
/// posture — default engine, egress allowlist, execution bounds — on one screen
/// instead of reconstructing it from scattered <c>GetValue</c> calls.
/// </summary>
public sealed class AutomationOptions
{
    public const string SectionName = "Automation";

    public BrowserOptions Browser { get; init; } = new();
}

public sealed class BrowserOptions
{
    public const string SectionName = "Browser";

    /// <summary>Engine used when a workflow asks for <see cref="BrowserEngineType.Default"/>.</summary>
    public BrowserEngineType DefaultEngine { get; init; } = BrowserEngineType.Playwright;

    public bool Headless { get; init; } = true;

    /// <summary>Per-action timeout. Semantic waits override it with their own.</summary>
    public TimeSpan ActionTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Per-session navigation timeout.</summary>
    public TimeSpan NavigationTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Hosts a session may navigate to, matched on host (optionally with
    /// <c>:port</c>). Empty ⇒ deny everything, which is the intended default:
    /// an unconfigured deployment must not be able to reach anywhere (ADR-011 D1).
    /// </summary>
    public IReadOnlyList<string> AllowedHosts { get; init; } = [];

    /// <summary>Hard ceiling on one run's wall-clock time. Bounded execution, D6.</summary>
    public TimeSpan MaxRunDuration { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>Hard ceiling on navigations per run. Counters SSRF + runaway crawls.</summary>
    public int MaxNavigationsPerRun { get; init; } = 50;

    /// <summary>Hard ceiling on actions per run.</summary>
    public int MaxActionsPerRun { get; init; } = 500;
}
