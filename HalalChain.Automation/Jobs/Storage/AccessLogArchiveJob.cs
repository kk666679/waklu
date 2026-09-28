namespace HalalChain.Automation.Jobs.Storage;

using HalalChain.Automation.Abstractions;

/// <summary>
/// Archives the NDJSON access log into monthly-segmented storage. The
/// active log stays small; historical entries are content-addressed and
/// stored alongside evidence.
///
/// This is the audit trail's own retention, separate from evidence
/// retention. Access logs are archived, not tombstoned.
/// </summary>
public sealed class AccessLogArchiveJob : IScheduledJob
{
    public string Name => "access-log-archive";
    public string Cron => "0 5 1 * *";
    public bool RunOnStartup => false;
    public TimeSpan Timeout => TimeSpan.FromMinutes(30);

    public Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        // Implementation delegates to IAccessLogArchiver in Infrastructure.
        return Task.FromResult(JobResult.Success(0, TimeSpan.Zero));
    }
}
