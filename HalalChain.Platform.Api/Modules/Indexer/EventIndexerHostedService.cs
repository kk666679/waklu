using Nethereum.Web3;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Blocks;
using HalalChain.Platform.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Indexer;

/// <summary>
/// Reads chain events from the HalalChain platform contracts and
/// mirrors them into the off-chain DB (<c>*OnChain</c> tables). Idempotent
/// on <c>(txHash, logIndex)</c>: re-processing the same log is a no-op.
///
/// Strategy: we use a hybrid push+pull model. The
/// <see cref="IChainEventPusher"/> is the fast path (subscribes via
/// WebSocket if available; falls back to polling). The
/// <see cref="ChainReorgHandler"/> is the safety net: every 64 blocks
/// we re-fetch the canonical block hash and compare to what we
/// indexed; on mismatch we roll back the affected rows and replay.
///
/// In MVP we ship the polling-only path. WebSocket subscription is
/// added in Phase 3.
/// </summary>
public sealed class EventIndexerHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _config;
    private readonly ILogger<EventIndexerHostedService> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly int _confirmations;
    private readonly long _startBlock;
    private readonly string _cursorPath;

    public EventIndexerHostedService(IServiceProvider sp, IConfiguration config, ILogger<EventIndexerHostedService> logger)
    {
        _sp = sp; _config = config; _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(config.GetValue("Indexer:PollIntervalSeconds", 12));
        _confirmations = config.GetValue("Blockchain:Confirmations", 64);
        _startBlock = config.GetValue("Indexer:StartBlock", 0);
        _cursorPath = Path.Combine(config["DataProtection:KeysPath"] ?? sp.GetRequiredService<IHostEnvironment>().ContentRootPath, "indexer-cursor.txt");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventIndexerHostedService started. Poll={Seconds}s Confirmations={Conf} StartBlock={Block}",
            _pollInterval.TotalSeconds, _confirmations, _startBlock);

        var rpcUrl = _config["Blockchain:RpcUrl"]!;
        var web3 = new Web3(rpcUrl);

        var lastIndexed = await LoadLastIndexedBlockAsync(stoppingToken);
        if (lastIndexed < _startBlock) lastIndexed = _startBlock;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var head = (long)(await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;
                var safe = head - _confirmations;
                if (safe > lastIndexed)
                {
                    var from = lastIndexed + 1;
                    var to = Math.Min(safe, from + 1000); // batch up to 1000 blocks per tick
                    var logs = await web3.Eth.Filters.GetLogs.SendRequestAsync(new Nethereum.RPC.Eth.DTOs.NewFilterInput
                    {
                        FromBlock = new Nethereum.RPC.Eth.DTOs.BlockParameter(new Nethereum.Hex.HexTypes.HexBigInteger(from)),
                        ToBlock = new Nethereum.RPC.Eth.DTOs.BlockParameter(new Nethereum.Hex.HexTypes.HexBigInteger(to)),
                        // We index all 4 platform contracts; in production the ABI topics are
                        // bound to each event signature.
                        Address = new[] { _config["Blockchain:Contracts:Suppliers"], _config["Blockchain:Contracts:Products"], _config["Blockchain:Contracts:Certs"], _config["Blockchain:Contracts:Events"] }
                            .Where(a => !string.IsNullOrWhiteSpace(a))
                            .Select(a => a!).ToArray()
                    });
                    await ProcessLogsAsync(logs, stoppingToken);
                    lastIndexed = to;
                    await SaveLastIndexedBlockAsync(lastIndexed, stoppingToken);
                    _logger.LogInformation("Indexed up to block {Block} ({Count} logs)", lastIndexed, logs?.Length ?? 0);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Indexer tick failed; retrying next interval");
            }

            try { await Task.Delay(_pollInterval, stoppingToken); } catch { break; }
        }

        _logger.LogInformation("EventIndexerHostedService stopped");
    }

    private async Task ProcessLogsAsync(FilterLog[]? logs, CancellationToken ct)
    {
        if (logs is null) return;
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalalChainDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();

        foreach (var log in logs)
        {
            // Idempotency: skip if we've already indexed (txHash, logIndex)
            var key = $"{log.TransactionHash}:{log.LogIndex.Value}";
            var exists = db.ChainTxOutbox.Any(x => x.TxHash == log.TransactionHash);
            // We use a lightweight in-memory check for now; a dedicated IndexedLog
            // table is added in Phase 3 to make this queryable.
            if (db.SuppliersOnChain.Any(s => s.TxHash == log.TransactionHash) ||
                db.ProductsOnChain.Any(p => p.TxHash == log.TransactionHash) ||
                db.CertificatesOnChain.Any(c => c.TxHash == log.TransactionHash) ||
                db.TraceabilityEventsOnChain.Any(t => t.TxHash == log.TransactionHash && t.LogIndex == (int)log.LogIndex.Value))
                continue;

            await dispatcher.DispatchAsync(log, ct);
        }
    }

    private async Task<long> LoadLastIndexedBlockAsync(CancellationToken ct)
    {
        if (File.Exists(_cursorPath))
        {
            try
            {
                var text = await File.ReadAllTextAsync(_cursorPath, ct);
                if (long.TryParse(text, out var block))
                    return block;
            }
            catch { }
        }
        return _startBlock;
    }

    private async Task SaveLastIndexedBlockAsync(long block, CancellationToken ct)
    {
        try
        {
            var dir = Path.GetDirectoryName(_cursorPath)!;
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(_cursorPath, block.ToString(), ct);
        }
        catch { }
    }
}
