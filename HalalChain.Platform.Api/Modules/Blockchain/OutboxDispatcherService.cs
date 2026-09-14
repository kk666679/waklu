namespace HalalChain.Platform.Api.Modules.Blockchain;

/// <summary>
/// Background worker that drains the <c>ChainTxOutbox</c>. Runs every
/// <c>Blockchain:OutboxPollSeconds</c> seconds (default 5s). Bounded
/// batch size (<c>OutboxBatchSize</c>, default 25) prevents one slow
/// RPC from blocking the rest.
///
/// The worker is deliberately simple: the outbox is the source of
/// truth, the worker is just a pump. Crashes are recoverable — the
/// next tick resumes from where it left off.
/// </summary>
public sealed class OutboxDispatcherService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _config;
    private readonly ILogger<OutboxDispatcherService> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly int _batchSize;

    public OutboxDispatcherService(IServiceProvider sp, IConfiguration config, ILogger<OutboxDispatcherService> logger)
    {
        _sp = sp; _config = config; _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(config.GetValue("Blockchain:OutboxPollSeconds", 5));
        _batchSize = config.GetValue("Blockchain:OutboxBatchSize", 25);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcherService started. Poll={Seconds}s Batch={Batch}",
            _pollInterval.TotalSeconds, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<ITransactionQueue>();
                var processed = await queue.ProcessPendingAsync(_batchSize, stoppingToken);
                if (processed > 0)
                    _logger.LogInformation("Outbox dispatched {Count} tx(s)", processed);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher tick failed; will retry next interval");
            }

            try { await Task.Delay(_pollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("OutboxDispatcherService stopped");
    }
}
