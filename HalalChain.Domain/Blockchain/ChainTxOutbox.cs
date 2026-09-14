namespace HalalChain.Domain.Blockchain;

/// <summary>
/// Durable outbox for chain transactions. Every <c>Enqueue</c> writes a row here
/// synchronously so the promise-to-write survives a process crash.
/// </summary>
public sealed class ChainTxOutbox
{
    public Guid Id { get; set; }
    public string FunctionName { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty; // SHA-256 of (function, target, data) — used for idempotency
    public string TxHash { get; set; } = string.Empty; // "0xpending..." until submitted, then real hash
    public string Status { get; set; } = "Requested"; // Requested | Pending | Confirmed | Failed | Reverted
    public int? BlockNumber { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? Error { get; set; }
}
