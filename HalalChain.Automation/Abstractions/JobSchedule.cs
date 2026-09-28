namespace HalalChain.Automation.Abstractions;

/// <summary>
/// Static schedule metadata for a job. Separated from IScheduledJob so the
/// schedule can be inspected without instantiating the job.
/// </summary>
public sealed record JobSchedule(
    string Name,
    string Cron,
    bool RunOnStartup,
    TimeSpan Timeout);
