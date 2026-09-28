namespace HalalChain.Automation.Scheduling;

using Cronos;

/// <summary>
/// Wraps a cron expression. All times are UTC. The next occurrence is
/// computed from the current clock, never from wall-clock time.
/// </summary>
public sealed class CronSchedule
{
    private readonly CronExpression _expression;

    public CronSchedule(string cron)
    {
        _expression = CronExpression.Parse(cron, CronFormat.Standard);
    }

    public DateTimeOffset NextOccurrence(DateTimeOffset from) =>
        _expression.GetNextOccurrence(from, TimeZoneInfo.Utc)
        ?? DateTimeOffset.MaxValue;
}
