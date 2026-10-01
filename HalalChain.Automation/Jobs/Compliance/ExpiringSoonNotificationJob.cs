namespace HalalChain.Automation.Jobs.Compliance;

using HalalChain.Application.Halal.Interfaces;
using HalalChain.Automation.Abstractions;

/// <summary>
/// Sends a renewal reminder to vendors whose certificates are approaching
/// expiry. Runs daily at 09:00 UTC, which is 17:00 in Malaysia — end of
/// the business day, when the reminder will be seen.
///
/// Delegates to the notification service. Does not compute anything.
/// </summary>
public sealed class ExpiringSoonNotificationJob : IScheduledJob
{
    private readonly ICertificateRepository _certificates;
    private readonly INotificationDispatcher _notifications;

    public ExpiringSoonNotificationJob(
        ICertificateRepository certificates,
        INotificationDispatcher notifications)
    {
        _certificates = certificates;
        _notifications = notifications;
    }

    public string Name => "expiring-soon-notification";
    public string Cron => "0 9 * * *";
    public bool RunOnStartup => false;
    public TimeSpan Timeout => TimeSpan.FromMinutes(5);

    public async Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        var now = ctx.Now;
        var horizon = now.AddDays(30);

        var expiring = await _certificates.QueryExpiringAsync(now, horizon, ct);

        foreach (var certificate in expiring)
        {
            await _notifications.SendCertificateExpiryReminderAsync(
                certificate.ProductId,
                certificate.CertificateNumber,
                certificate.ExpiryDate,
                ct);
        }

        return JobResult.Success(expiring.Count, TimeSpan.Zero);
    }
}
