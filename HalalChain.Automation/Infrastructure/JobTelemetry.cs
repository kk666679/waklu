namespace HalalChain.Automation.Infrastructure;

using System.Diagnostics.Metrics;

/// <summary>
/// OpenTelemetry meters for job runs. Emitted per run, not per tick —
/// a skipped job does not emit a metric.
/// </summary>
public static class JobTelemetry
{
    public const string MeterName = "HalalChain.Automation";

    public static readonly Meter Meter = new(MeterName);
    public static readonly Counter<long> JobsCompleted =
        Meter.CreateCounter<long>("jobs.completed", description: "Jobs completed, tagged by name and outcome.");
    public static readonly Histogram<double> JobDuration =
        Meter.CreateHistogram<double>("jobs.duration", unit: "ms", description: "Job duration in milliseconds.");
    public static readonly Counter<long> JobsSkipped =
        Meter.CreateCounter<long>("jobs.skipped", description: "Jobs skipped because the lock was held.");
}
