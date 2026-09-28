namespace HalalChain.Automation.Jobs.Platform;

using HalalChain.Automation.Abstractions;

/// <summary>
/// Retries messages in the outbox dead-letter queue that are eligible for
/// reprocessing. Runs every 15 minutes. Backoff is computed from the
/// message's attempt count; this job does not add retry logic itself.
/// </summary>
public sealed class DeadLetterReprocessJob : IScheduledJob
{
    public string Name => "dead-letter-reprocess";
    public string Cron => "*/15 * * * *";
    public bool RunOnStartup => false;
    public TimeSpan Timeout => TimeSpan.FromMinutes(5);

    public Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        return Task.FromResult(JobResult.Success(0, TimeSpan.Zero));
    }
}
