namespace HalalChain.Automation.Jobs.Storage;

using HalalChain.Application.Storage;
using HalalChain.Automation.Abstractions;

/// <summary>
/// Applies the retention policy to the evidence store. Tombstones records
/// past their window; never deletes blobs. The blob purge, where it exists,
/// is a separate and separately-audited job.
///
/// Runs weekly on Sunday at 04:00 UTC — off-hours, low contention.
/// </summary>
public sealed class EvidenceRetentionSweepJob : IScheduledJob
{
    private readonly IEvidenceStore _evidence;

    public EvidenceRetentionSweepJob(IEvidenceStore evidence) => _evidence = evidence;

    public string Name => "evidence-retention-sweep";
    public string Cron => "0 4 * * 0";
    public bool RunOnStartup => false;
    public TimeSpan Timeout => TimeSpan.FromHours(2);

    public async Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        var policy = new RetentionPolicy(
            Window: TimeSpan.FromDays(2555), // 7 years, matching the config default
            Scope: RetentionScope.All);

        var report = await _evidence.ApplyRetentionAsync(policy, ct);

        return JobResult.Success(
            items: report.Evaluated,
            duration: TimeSpan.Zero,
            detail: $"Kept={report.Kept}, Extended={report.Extended}, Tombstoned={report.Tombstoned}");
    }
}
