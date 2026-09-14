using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;

namespace HalalChain.Platform.Api.Modules.Blockchain;

/// <summary>
/// Durable outbox + dispatcher. Every write to the chain goes through
/// here. The flow:
///
///   1. <see cref="EnqueueAsync"/> persists the tx request to the
///      <c>ChainTxOutbox</c> table (synchronous, durable).
///   2. A background dispatcher loop reads pending rows, signs with
///      <see cref="IWalletProvider"/>, sends via Nethereum, polls for
///      the receipt, and updates the row to Confirmed / Failed /
///      Reverted.
///   3. The caller receives the <see cref="ChainTxEnqueued"/> with
///      the assigned tx hash so it can return immediately to the API
///      caller. The actual on-chain write happens asynchronously; the
///      indexer (Indexer module) updates the off-chain mirror when
///      the corresponding event is observed.
///
/// The outbox is the single source of truth for "tx we promised to
/// send". A process crash between Enqueue and Send is recoverable:
/// on restart, the dispatcher resumes.
/// </summary>
public interface ITransactionQueue
{
    Task<ChainTxEnqueued> EnqueueAsync(string functionName, string target, TransactionInput tx, CancellationToken ct);
    Task<int> ProcessPendingAsync(int max, CancellationToken ct); // returns number processed
    Task<ChainTxStatus?> GetStatusAsync(string txHash, CancellationToken ct);
}

public sealed record ChainTxEnqueued(string TxHash, string Status, int? BlockNumber, DateTime SubmittedAt, string? Error);

public sealed record ChainTxStatus(string TxHash, string Status, int? BlockNumber, DateTime SubmittedAt, string? Error);
