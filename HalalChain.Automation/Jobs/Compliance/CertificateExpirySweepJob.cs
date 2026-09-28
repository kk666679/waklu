namespace HalalChain.Automation.Jobs.Compliance;

using HalalChain.Application.Halal.Commands;
using HalalChain.Automation.Abstractions;
using MediatR;

/// <summary>
/// The deterministic daily sweep. This is the job that makes "certification
/// is a state, not a snapshot" true: it transitions products to ExpiringSoon
/// or Suspended based on certificate expiry.
///
/// It runs once per day at 02:00 UTC. It does NOT run on startup — running
/// it on every deployment would produce a burst of state transitions during
/// rolling deploys, which is harmless but noisy in the audit log.
///
/// The job delegates entirely to SweepExpiringCertificatesCommand. No logic
/// lives here beyond the trigger.
/// </summary>
public sealed class CertificateExpirySweepJob : IScheduledJob
{
    private readonly ISender _sender;

    public CertificateExpirySweepJob(ISender sender) => _sender = sender;

    public string Name => "certificate-expiry-sweep";
    public string Cron => "0 2 * * *";
    public bool RunOnStartup => false;
    public TimeSpan Timeout => TimeSpan.FromMinutes(10);

    public async Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        var result = await _sender.Send(
            new SweepExpiringCertificatesCommand(LookaheadDays: 30), ct);

        return JobResult.Success(
            items: result.Evaluated,
            duration: TimeSpan.Zero,
            detail: $"Activated={result.Activated}, ExpiringSoon={result.ExpiringSoon}, Suspended={result.Suspended}, Skipped={result.Skipped}");
    }
}
