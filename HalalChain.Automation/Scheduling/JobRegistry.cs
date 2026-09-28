namespace HalalChain.Automation.Scheduling;

using HalalChain.Automation.Abstractions;

/// <summary>
/// Holds the collection of registered jobs. Populated at startup from DI.
/// Jobs run independently — there is no ordering guarantee, and no job
/// depends on another having run first.
/// </summary>
public sealed class JobRegistry
{
    private readonly List<IScheduledJob> _jobs = [];

    public IReadOnlyList<IScheduledJob> Jobs => _jobs.AsReadOnly();

    public void Register(IScheduledJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (_jobs.Any(j => j.Name == job.Name))
            throw new InvalidOperationException($"A job named '{job.Name}' is already registered.");
        _jobs.Add(job);
    }
}
