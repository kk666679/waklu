using HalalChain.Automation.Abstractions;
using HalalChain.Automation.Health;
using HalalChain.Automation.Infrastructure;
using HalalChain.Automation.Jobs.Agents;
using HalalChain.Automation.Jobs.Blockchain;
using HalalChain.Automation.Jobs.Compliance;
using HalalChain.Automation.Jobs.Platform;
using HalalChain.Automation.Jobs.Storage;
using HalalChain.Automation.Scheduling;

var builder = Host.CreateApplicationBuilder(args);

// ─── Aspire service defaults ─────────────────────────────────────────────────
builder.AddServiceDefaults();

// ─── Scheduling infrastructure ───────────────────────────────────────────────
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<JobRegistry>();
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
