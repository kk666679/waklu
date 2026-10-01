using HalalChain.Automation.Abstractions;
using HalalChain.Automation.Health;
using HalalChain.Automation.Infrastructure;
using HalalChain.Automation.Jobs.Agents;
using HalalChain.Automation.Jobs.Blockchain;
using HalalChain.Automation.Jobs.Compliance;
using HalalChain.Automation.Jobs.Platform;
using HalalChain.Automation.Jobs.Storage;
using HalalChain.Automation.Scheduling;

using Npgsql;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

// ─── Observability ───────────────────────────────────────────────────────────
// Service defaults: OpenTelemetry metrics and traces over OTLP, exported from
// the same meter the scheduler emits through. The OTLP endpoint is read from
// configuration; when it is absent the exporter stays inert rather than
// throwing, so a local run needs no collector.
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter(JobTelemetry.MeterName)
        .AddOtlpExporter())
    .WithTracing(tracing => tracing
        .AddOtlpExporter());

// ─── Scheduling infrastructure ───────────────────────────────────────────────
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<JobRegistry>();

// NpgsqlAdvisoryLock depends on an NpgsqlDataSource rather than a connection
// string, so the host has to own one data source for the process lifetime. It
// is built (not opened) here, so registration does not require a live database —
// the first actual connection is made when a job first tries to take the lock.
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "Connection string 'Postgres' is required: NpgsqlAdvisoryLock backs IDistributedLock with pg_try_advisory_lock.");
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(postgresConnectionString).Build());

builder.Services.AddSingleton<IDistributedLock, NpgsqlAdvisoryLock>();
builder.Services.AddHostedService<JobScheduler>();

// ─── Job registrations ───────────────────────────────────────────────────────
// Each job is registered as a singleton. Registration order has no
// meaning — the scheduler reads the registry, not DI enumeration.
RegisterJob<CertificateExpirySweepJob>(builder);
RegisterJob<ExpiringSoonNotificationJob>(builder);
RegisterJob<SuspendedListingCleanupJob>(builder);
RegisterJob<EvidenceRetentionSweepJob>(builder);
RegisterJob<AccessLogArchiveJob>(builder);
RegisterJob<AnchorCadenceJob>(builder);
RegisterJob<ChainMirrorSyncJob>(builder);
RegisterJob<OutboxDispatchJob>(builder);
RegisterJob<PeriodicRevalidationJob>(builder);
RegisterJob<EscalationReminderJob>(builder);
RegisterJob<DeadLetterReprocessJob>(builder);

// ─── Health ──────────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<AutomationHealthCheck>("automation");

var host = builder.Build();
host.Run();

static void RegisterJob<TJob>(HostApplicationBuilder builder)
    where TJob : class, IScheduledJob
{
    builder.Services.AddSingleton<TJob>();
    builder.Services.AddSingleton<IScheduledJob>(sp => sp.GetRequiredService<TJob>());
    builder.Services.AddSingleton(sp =>
    {
        // Populate the registry at container build time so the scheduler
        // sees every job before the host starts.
        var registry = sp.GetRequiredService<JobRegistry>();
        var job = sp.GetRequiredService<IScheduledJob>();
        if (!registry.Jobs.Any(j => j.Name == job.Name))
            registry.Register(job);
        return job;
    });
}
