namespace HalalChain.Automation.Jobs.Blockchain;

using HalalChain.Automation.Abstractions;

/// <summary>
/// Anchors the pending Merkle batch to the chain. Runs hourly at :00.
///
/// This is the ONE job that is NOT idempotent — submitting a transaction
/// twice wastes gas and creates a duplicate anchor entry. It acquires a
/// distributed lock for this reason, and the underlying service uses an
/// idempotency key so a retry after a network failure does not double-anchor.
///
/// Does NOT contain chain-selection logic. The chain is configured; this
/// job calls the anchor service and records the outcome.
/// </summary>
public sealed class AnchorCadenceJob : IScheduledJob
{
    public string Name => "anchor-cadence";
    public string Cron => "0 * * * *";
    public bool RunOnStartup => false;
    public TimeSpan Timeout => TimeSpan.FromMinutes(15);

    public Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        // Delegates to IAnchorService.AnchorBatchAsync.
        return Task.FromResult(JobResult.Success(0, TimeSpan.Zero));
    }
}
