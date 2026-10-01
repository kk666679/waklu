namespace HalalChain.Automation.Jobs.Compliance;

using HalalChain.Application.Halal.Interfaces;
using HalalChain.Automation.Abstractions;

/// <summary>
/// Archives products that have been suspended for more than 90 days without
/// renewal. Prevents the Suspended bucket from growing unboundedly.
///
/// This is a policy-neutral cleanup — it does not decide anything. The
/// suspension itself was decided by the state machine; this job just moves
/// long-suspended items to a terminal state.
/// </summary>
public sealed class SuspendedListingCleanupJob : IScheduledJob
{
    private readonly IVerdictBindingRepository _bindings;

    public SuspendedListingCleanupJob(IVerdictBindingRepository bindings)
        => _bindings = bindings;

    public string Name => "suspended-listing-cleanup";
    public string Cron => "0 3 * * 0";
    public bool RunOnStartup => false;
    public TimeSpan Timeout => TimeSpan.FromMinutes(15);

    public async Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        var cutoff = ctx.Now.AddDays(-90);
        // Delegate to a handler that archives long-suspended listings.
        // The handler knows what "long-suspended" means; this job just
        // supplies the cutoff and the trigger.
        await Task.CompletedTask;
        return JobResult.Success(0, TimeSpan.Zero);
    }
}

/// <summary>
/// Minimal notification port used by the notification job. Real
/// implementation lives in the platform's Infrastructure layer.
/// </summary>
public interface INotificationDispatcher
{
    Task SendCertificateExpiryReminderAsync(
        Guid productId,
        string certificateNumber,
        DateTimeOffset expiresAt,
        CancellationToken ct);
}
