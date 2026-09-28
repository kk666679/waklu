namespace HalalChain.Automation.Abstractions;

public enum JobOutcome
{
    Success,
    Skipped,
    Failed,
}

public sealed record JobResult(
    JobOutcome Outcome,
    int ItemsProcessed,
    string? Detail,
    TimeSpan Duration)
{
    public static JobResult Success(int items, TimeSpan duration, string? detail = null)
        => new(JobOutcome.Success, items, detail, duration);

    public static JobResult Skipped(string reason, TimeSpan duration)
        => new(JobOutcome.Skipped, 0, reason, duration);

    public static JobResult Failed(string reason, TimeSpan duration)
        => new(JobOutcome.Failed, 0, reason, duration);
}
