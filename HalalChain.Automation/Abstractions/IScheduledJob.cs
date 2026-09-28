namespace HalalChain.Automation.Abstractions;

/// <summary>
/// A scheduled job. Implementations delegate to an Application-layer
/// handler or a port. They do not contain policy logic.
///
/// The test: if you deleted the schedule and called the same handler
/// synchronously, the outcome would be identical. If timing is part of
/// the decision, the logic belongs in Application and this interface
/// is just a trigger.
/// </summary>
public interface IScheduledJob
{
    /// <summary>Stable name for logs, metrics, and distributed locks.</summary>
    string Name { get; }

    /// <summary>How often the job runs. Cron, in UTC.</summary>
    string Cron { get; }

    /// <summary>If true, the job runs once on startup before the first cron tick.</summary>
    bool RunOnStartup { get; }

    /// <summary>Maximum time the job is allowed to run before it is cancelled.</summary>
    TimeSpan Timeout { get; }

    Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct);
}
