namespace HalalChain.Platform.Api.Observability;

/// <summary>
/// Business metrics for the HalalChain Platform API.
/// Meters are created via OpenTelemetry and exposed on the /metrics endpoint.
/// </summary>
public static class PlatformMetrics
{
    public const string MeterName = "HalalChain.Platform";

    private static readonly System.Diagnostics.Metrics.Meter Meter = new(MeterName);

    // ── Order metrics ─────────────────────────────────────────────────────
    public static readonly System.Diagnostics.Metrics.Counter<long> OrdersCreated =
        Meter.CreateCounter<long>("halalchain_orders_created_total",
            description: "Total orders created");

    public static readonly System.Diagnostics.Metrics.Histogram<double> OrderTotalValueUsd =
        Meter.CreateHistogram<double>("halalchain_orders_total_value_usd",
            description: "Order total value in USD");

    // ── Outbox metrics ────────────────────────────────────────────────────
    public static readonly System.Diagnostics.Metrics.Counter<long> OutboxDispatched =
        Meter.CreateCounter<long>("outbox_dispatched_total",
            description: "Total outbox messages dispatched");

    public static readonly System.Diagnostics.Metrics.Counter<long> OutboxDeadLettered =
        Meter.CreateCounter<long>("outbox_dead_lettered_total",
            description: "Total outbox messages dead-lettered");

    public static readonly System.Diagnostics.Metrics.Gauge<long> OutboxPending =
        Meter.CreateGauge<long>("outbox_pending_total",
            description: "Current pending outbox messages");

    public static readonly System.Diagnostics.Metrics.Gauge<double> OutboxOldestTimestamp =
        Meter.CreateGauge<double>("outbox_oldest_pending_timestamp_seconds",
            description: "Seconds since oldest pending outbox message");
}