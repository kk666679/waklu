namespace HalalChain.Domain.Blockchain;

using HalalChain.Domain.Common;

/// <summary>
/// A transaction the platform intends to submit to the chain.
///
/// This is the write-side of the mirror. The application enqueues intents,
/// a background worker signs and submits them, and the result is recorded
/// as a ChainMirror entry.
///
/// Idempotency is by IdempotencyKey: resubmitting with the same key is a
/// no-op. This is how the platform handles chain congestion and retries
/// without double-anchoring.
/// </summary>
public sealed class ChainTxOutbox : Entity<Guid>
{
    public string IdempotencyKey { get; private init; } = string.Empty;
    public string ContractName { get; private init; } = string.Empty;
    public string MethodName { get; private init; } = string.Empty;
    public string PayloadJson { get; private init; } = string.Empty;
    public ChainTxStatus Status { get; private set; } = ChainTxStatus.Pending;
    public DateTimeOffset EnqueuedAt { get; private init; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public string? TransactionHash { get; private set; }
    public string? Error { get; private set; }
    public int Attempts { get; private set; }

    private ChainTxOutbox() { }

    public static ChainTxOutbox Enqueue(
        string idempotencyKey,
        string contractName,
        string methodName,
        string payloadJson,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contractName);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        return new ChainTxOutbox
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            ContractName = contractName,
            MethodName = methodName,
            PayloadJson = payloadJson,
            Status = ChainTxStatus.Pending,
            EnqueuedAt = now,
        };
    }

    public void MarkSubmitted(string txHash, DateTimeOffset now)
    {
        Status = ChainTxStatus.Submitted;
        TransactionHash = txHash;
        SubmittedAt = now;
    }

    public void MarkConfirmed(DateTimeOffset now)
    {
        Status = ChainTxStatus.Confirmed;
        ConfirmedAt = now;
    }

    public void RecordFailure(string error)
    {
        Error = error;
        Attempts++;
        if (Attempts >= 5)
            Status = ChainTxStatus.Failed;
    }
}

public enum ChainTxStatus
{
    Pending,
    Submitted,
    Confirmed,
    Failed,
}
