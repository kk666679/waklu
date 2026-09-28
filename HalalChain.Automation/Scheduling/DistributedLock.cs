namespace HalalChain.Automation.Scheduling;

/// <summary>
/// Prevents two replicas of the Automation process from running the same
/// job at the same time. Backed by a Postgres advisory lock.
///
/// Jobs that are already idempotent do not need this. The expiry sweep is
/// idempotent (it computes transitions from current state); the anchor
/// cadence job is not (it submits transactions). Locking is opt-in.
/// </summary>
public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        string key,
        TimeSpan timeout,
        CancellationToken ct);
}
