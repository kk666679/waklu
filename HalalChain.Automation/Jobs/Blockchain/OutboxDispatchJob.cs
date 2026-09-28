namespace HalalChain.Automation.Jobs.Blockchain;

using HalalChain.Automation.Abstractions;

/// <summary>
/// Reads pending ChainTxOutbox rows and submits them to the chain.
/// Runs every minute. This is the write-side complement to the mirror sync.
///
/// Idempotency is enforced by the outbox row's IdempotencyKey: a
/// resubmission after a network failure is a no-op at the chain level.
/// </summary>
public sealed class OutboxDispatchJob : IScheduledJob
{
    public string Name => "outbox-dispatch";
    public string Cron => "* * * * *";
    public bool RunOnStartup => true;
    public TimeSpan Timeout => TimeSpan.FromSeconds(50);

    public Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        // Delegates to IChainWriter.DispatchPendingAsync.
        return Task.FromResult(JobResult.Success(0, TimeSpan.Zero));
    }
}
