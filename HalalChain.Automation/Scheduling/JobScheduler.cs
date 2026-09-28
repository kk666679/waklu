namespace HalalChain.Automation.Scheduling;

using System.Diagnostics;
using HalalChain.Automation.Abstractions;
using Microsoft.Extensions.Hosting;

/// <summary>
/// The hosted service that runs registered jobs on their cron schedules.
///
/// Design notes:
///   - Jobs run sequentially within a tick, not in parallel. Parallel
///     execution across replicas is handled by the distributed lock.
///   - A job that exceeds its timeout is cancelled and recorded as Failed.
///   - A job that throws is caught, recorded, and does not stop the host.
///     The host only stops on shutdown.
/// </summary>
public sealed class JobScheduler : BackgroundService
{
    private readonly JobRegistry _registry;
    private readonly IServiceProvider _services;
    private readonly IDistributedLock _lock;
    private readonly TimeProvider _clock;
    private readonly ILogger<JobScheduler> _logger;
    private readonly Dictionary<string, CronSchedule> _schedules;

    public JobScheduler(
        JobRegistry registry,
        IServiceProvider services,
        IDistributedLock distributedLock,
        TimeProvider clock,
        ILogger<JobScheduler> logger)
    {
        _registry = registry;
        _services = services;
        _lock = distributedLock;
        _clock = clock;
        _logger = logger;
        _schedules = registry.Jobs.ToDictionary(
            j => j.Name,
            j => new CronSchedule(j.Cron));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run startup jobs first. These are the ones whose correctness
        // depends on having run at least once since the last deployment.
        foreach (var job in _registry.Jobs.Where(j => j.RunOnStartup))
        {
            await RunJobAsync(job, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = _clock.GetUtcNow();
            var nextTick = _schedules.Values
                .Select(s => s.NextOccurrence(now))
                .DefaultIfEmpty(DateTimeOffset.MaxValue)
                .Min();

            var delay = nextTick - now;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, _clock, stoppingToken);

            foreach (var job in _registry.Jobs)
            {
                if (_schedules[job.Name].NextOccurrence(_clock.GetUtcNow()) <= nextTick)
                    await RunJobAsync(job, stoppingToken);
            }
        }
    }

    private async Task RunJobAsync(IScheduledJob job, CancellationToken stoppingToken)
    {
        await using var jobLock = await _lock.TryAcquireAsync(
            $"job:{job.Name}", job.Timeout, stoppingToken);

        if (jobLock is null)
        {
            _logger.LogInformation(
                "Job {Job} skipped — lock held by another replica.", job.Name);
            return;
        }

        var correlationId = Guid.NewGuid();
        using var scope = _services.CreateScope();
        var ctx = new JobContext
        {
            JobName = job.Name,
            CorrelationId = correlationId,
            Clock = _clock,
            Logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(job.Name),
            Services = scope.ServiceProvider,
        };

        var stopwatch = Stopwatch.StartNew();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(job.Timeout);

        try
        {
            var result = await job.RunAsync(ctx, timeoutCts.Token);
            stopwatch.Stop();
            _logger.LogInformation(
                "Job {Job} completed: {Outcome} ({Items} items) in {Duration}ms",
                job.Name, result.Outcome, result.ItemsProcessed, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Job {Job} timed out after {Timeout}.", job.Name, job.Timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {Job} failed.", job.Name);
        }
    }
}
