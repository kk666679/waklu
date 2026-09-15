namespace HalalChain.Platform.Api.Observability;

/// <summary>
/// Outbox-specific metrics helper.
/// </summary>
public static class OutboxMetrics
{
    public static readonly System.Diagnostics.Metrics.Counter<long> Dispatched =
        PlatformMetrics.OutboxDispatched;

    public static readonly System.Diagnostics.Metrics.Counter<long> DeadLettered =
        PlatformMetrics.OutboxDeadLettered;

    public static readonly System.Diagnostics.Metrics.Gauge<long> Pending =
        PlatformMetrics.OutboxPending;

    public static readonly System.Diagnostics.Metrics.Gauge<double> OldestTimestamp =
        PlatformMetrics.OutboxOldestTimestamp;
}