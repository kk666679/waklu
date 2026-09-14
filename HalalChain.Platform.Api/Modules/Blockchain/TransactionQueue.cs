using HalalChain.Domain.Blockchain;
using HalalChain.Platform.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;
using System.Security.Cryptography;
using System.Text;

namespace HalalChain.Platform.Api.Modules.Blockchain;

/// <summary>
/// Default <see cref="ITransactionQueue"/> implementation. Persists every
/// tx request to the <c>ChainTxOutbox</c> table (durable) and exposes
/// a dispatcher that signs, sends, and polls for the receipt.
///
/// The outbox row carries a SHA-256 of (functionName, target, data,
/// requestFingerprint) as a deterministic key — if the caller enqueues
/// the same logical request twice, the outbox is a no-op (idempotency).
/// </summary>
public sealed class TransactionQueue : ITransactionQueue
{
    private readonly IServiceProvider _sp;
    private readonly IWalletProvider _wallet;
    private readonly IConfiguration _config;
    private readonly ILogger<TransactionQueue> _logger;
    private readonly TimeSpan _txTimeout;
    private readonly int _confirmations;

    public TransactionQueue(IServiceProvider sp, IWalletProvider wallet, IConfiguration config, ILogger<TransactionQueue> logger)
    {
        _sp = sp; _wallet = wallet; _config = config; _logger = logger;
        _txTimeout = TimeSpan.FromSeconds(config.GetValue("Blockchain:TxTimeoutSeconds", 180));
        _confirmations = config.GetValue("Blockchain:Confirmations", 64);
    }

    public async Task<ChainTxEnqueued> EnqueueAsync(string functionName, string target, TransactionInput tx, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalalChainDbContext>();
        var fingerprint = ComputeFingerprint(functionName, target, tx.Data ?? "");
        var existing = await db.ChainTxOutbox
            .Where(x => x.Fingerprint == fingerprint && x.Status != "Failed" && x.Status != "Reverted")
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            _logger.LogInformation("Outbox: duplicate request {Fingerprint} dedup'd to existing tx {TxHash}", fingerprint, existing.TxHash);
            return new ChainTxEnqueued(existing.TxHash, existing.Status, existing.BlockNumber, existing.SubmittedAt, existing.Error);
        }

        var txHash = ComputeTxHashPlaceholder(functionName, target, tx.Data ?? "");
        var row = new ChainTxOutbox
        {
            Id = Guid.NewGuid(),
            FunctionName = functionName,
            Target = target,
            Data = tx.Data ?? "",
            Fingerprint = fingerprint,
            TxHash = txHash,
            Status = "Requested",
            BlockNumber = null,
            SubmittedAt = DateTime.UtcNow,
            Error = null
        };
        db.ChainTxOutbox.Add(row);
        await db.SaveChangesAsync(ct);
        return new ChainTxEnqueued(txHash, "Requested", null, row.SubmittedAt, null);
    }

    public async Task<int> ProcessPendingAsync(int max, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalalChainDbContext>();
        var pending = await db.ChainTxOutbox
            .Where(x => x.Status == "Requested" || x.Status == "Pending")
            .OrderBy(x => x.SubmittedAt)
            .Take(max)
            .ToListAsync(ct);

        var processed = 0;
        var rpcUrl = _config["Blockchain:RpcUrl"]!;
        var web3 = new Web3(_wallet.GetAccount(), rpcUrl);

        foreach (var row in pending)
        {
            try
            {
                if (row.Status == "Requested")
                {
                    var input = new TransactionInput { To = row.Target, Data = row.Data, From = _wallet.Address };
                    // Estimate + cap gas
                    try
                    {
                        var estimate = await web3.Eth.TransactionManager.EstimateGasAsync(input);
                        input.Gas = new Nethereum.Hex.HexTypes.HexBigInteger(estimate.Value * 2);
                    }
                    catch { input.Gas = new Nethereum.Hex.HexTypes.HexBigInteger(500_000); }
                    var gasPrice = await new GasPriceOracle(_config).GetSuggestedAsync(ct);
                    if (gasPrice is not null) input.GasPrice = gasPrice;

                    var txHash = await web3.Eth.TransactionManager.SendTransactionAsync(input);
                    row.TxHash = txHash;
                    row.Status = "Pending";
                    row.SubmittedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("Submitted tx {TxHash} for {Function}", txHash, row.FunctionName);
                }

                if (row.Status == "Pending")
                {
                    var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(row.TxHash);
                    if (receipt is null)
                    {
                        if (DateTime.UtcNow - row.SubmittedAt > _txTimeout)
                        {
                            row.Status = "Failed";
                            row.Error = "Transaction timeout (no receipt within window)";
                            await db.SaveChangesAsync(ct);
                            _logger.LogWarning("Tx {TxHash} ({Function}) timed out", row.TxHash, row.FunctionName);
                        }
                        continue;
                    }
                    if (receipt.Status.Value == 0)
                    {
                        row.Status = "Reverted";
                        row.Error = "Transaction reverted on-chain";
                        await db.SaveChangesAsync(ct);
                        _logger.LogWarning("Tx {TxHash} reverted", row.TxHash);
                        continue;
                    }
                    // Wait for N confirmations
                    var currentBlock = (long)(await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;
                    var txBlock = (long)receipt.BlockNumber.Value;
                    if (currentBlock - txBlock + 1 < _confirmations) continue;

                    row.Status = "Confirmed";
                    row.BlockNumber = (int)receipt.BlockNumber.Value;
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("Tx {TxHash} confirmed in block {Block}", row.TxHash, row.BlockNumber);
                }
                processed++;
            }
            catch (Exception ex)
            {
                row.Status = "Failed";
                row.Error = ex.Message;
                await db.SaveChangesAsync(ct);
                _logger.LogError(ex, "Outbox row {Id} failed", row.Id);
            }
        }
        return processed;
    }

    public async Task<ChainTxStatus?> GetStatusAsync(string txHash, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HalalChainDbContext>();
        var row = await db.ChainTxOutbox.AsNoTracking().FirstOrDefaultAsync(x => x.TxHash == txHash, ct);
        return row is null ? null : new ChainTxStatus(row.TxHash, row.Status, row.BlockNumber, row.SubmittedAt, row.Error);
    }

    private static string ComputeFingerprint(string function, string target, string data)
    {
        var raw = Encoding.UTF8.GetBytes($"{function}|{target.ToLowerInvariant()}|{data}");
        var hash = SHA256.HashData(raw);
        return Convert.ToHexString(hash);
    }

    private static string ComputeTxHashPlaceholder(string function, string target, string data)
    {
        // We don't have the real tx hash until the dispatcher signs and sends.
        // We use a stable placeholder so the outbox row is identifiable before submission.
        var raw = Encoding.UTF8.GetBytes($"pending::{function}::{target}::{data}");
        var hash = SHA256.HashData(raw);
        return "0xpending" + Convert.ToHexString(hash)[..40].ToLowerInvariant();
    }
}
