namespace HalalChain.Domain.Common;

/// <summary>
/// Transactional outbox entry. Written in the same transaction as the
/// aggregate change, published asynchronously by a background worker.
///
/// This type carries no transport concerns — no serialization attributes,
/// no target system reference. Those live in infrastructure.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int Attempts { get; private set; }

    public static OutboxMessage Create(string type, string payload, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Payload = payload,
        OccurredAt = now,
    };

    public void MarkProcessed(DateTimeOffset now) => ProcessedAt = now;

    public void RecordFailure(string error)
    {
        Error = error;
        Attempts++;
    }
}
