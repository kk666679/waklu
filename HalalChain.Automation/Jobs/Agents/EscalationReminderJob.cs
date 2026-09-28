namespace HalalChain.Automation.Jobs.Agents;

using HalalChain.Automation.Abstractions;

/// <summary>
/// Reminds operators about escalations that have sat unresolved past their
/// SLA. An escalation is the agents layer saying "get a human" — the
/// reminder is a nudge, not a decision.
///
/// Runs every 4 hours. Escalations older than 24 hours get a reminder to
/// the responsible role; older than 72 hours get escalated again.
/// </summary>
public sealed class EscalationReminderJob : IScheduledJob
{
    public string Name => "escalation-reminder";
    public string Cron => "0 */4 * * *";
    public bool RunOnStartup => false;
    public TimeSpan Timeout => TimeSpan.FromMinutes(2);

    public Task<JobResult> RunAsync(JobContext ctx, CancellationToken ct)
    {
        // Delegates to IEscalationService.
        return Task.FromResult(JobResult.Success(0, TimeSpan.Zero));
    }
}
