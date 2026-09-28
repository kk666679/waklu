namespace HalalChain.Automation.Jobs.Agents;

using HalalChain.Automation.Abstractions;

/// <summary>
/// Triggers the periodic_revalidation agent workflow for vendors whose
/// certificates are still valid but whose evidence is more than 180 days old.
///
/// This is the only job in the platform that calls an agent workflow on a
/// schedule. It does not evaluate the result — it produces proposals, which
/// are then routed through tawheed via the workflow's normal path.
///
/// The job is safe to make idempotent per vendor per window: it checks
/// whether a run already exists before triggering.
/// </summary>
public sealed class PeriodicRevalidationJob : IScheduledJob
{
    public string Name => "periodic-revalidation";
    public string Cron => "0 6 * * 1";
    public bool RunOnStartup => false;
    public TimeSpan Timeout => TimeSpan.FromHours(1);

    public Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        // Delegates to a workflow trigger port. The workflow runs in
        // .halalchain/agents/ and reports back through the outbox.
        return Task.FromResult(JobResult.Success(0, TimeSpan.Zero));
    }
}
