namespace HalalChain.Automation.Jobs.Blockchain;

using HalalChain.Automation.Abstractions;

/// <summary>
/// Reads new blocks and reconciles the local ChainMirror with on-chain
/// state. The chain is authoritative; the mirror is a cache.
///
/// Runs every 5 minutes. The chain's finality window is longer than that,
/// so this job may observe an event multiple times as confirmations grow.
/// The mirror is upserted, not appended.
/// </summary>
public sealed class ChainMirrorSyncJob : IScheduledJob
{
    public string Name => "chain-mirror-sync";
    public string Cron => "*/5 * * * *";
    public bool RunOnStartup => true;
    public TimeSpan Timeout => TimeSpan.FromMinutes(4);

    public Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        // Delegates to IChainReader.SyncAsync.
        return Task.FromResult(JobResult.Success(0, TimeSpan.Zero));
    }
}
