namespace HalalChain.Automation.Browser;

using System.Diagnostics.Metrics;

/// <summary>
/// OpenTelemetry instruments for browser runs.
///
/// Follows <see cref="Infrastructure.JobTelemetry"/>: one meter per subsystem,
/// emitted per meaningful event rather than per tick. Registered by
/// <c>Program.cs</c> alongside the job meter so a single collector sees both
/// halves of an automation run.
///
/// Deliberately absent: URL query strings, form values, and any captured
/// content. Attributes are engine, host, kind, status — enough to answer "are
/// browser runs slow / flaky / blocked?" without ever shipping page data
/// (ADR-011 D7).
/// </summary>
public static class BrowserTelemetry
{
    public const string MeterName = "HalalChain.Automation.Browser";

    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> SessionsCreated =
        Meter.CreateCounter<long>(
            "browser.sessions.created",
            description: "Browser sessions opened, tagged by engine.");

    public static readonly Counter<long> SessionsClosed =
        Meter.CreateCounter<long>(
            "browser.sessions.closed",
            description: "Browser sessions disposed, tagged by engine.");

    public static readonly Histogram<double> SessionDuration =
        Meter.CreateHistogram<double>(
            "browser.sessions.duration",
            unit: "ms",
            description: "Wall-clock time a browser session was held open.");

    public static readonly Counter<long> ActionsCompleted =
        Meter.CreateCounter<long>(
            "browser.actions.completed",
            description: "Browser actions finished, tagged by action kind and status.");

    public static readonly Counter<long> NavigationsBlocked =
        Meter.CreateCounter<long>(
            "browser.navigations.blocked",
            description: "Navigations rejected by the egress allowlist (ADR-011 D1).");

    public static readonly Counter<long> NavigationsCompleted =
        Meter.CreateCounter<long>(
            "browser.navigations.completed",
            description: "Navigations that completed, tagged by host.");
}
