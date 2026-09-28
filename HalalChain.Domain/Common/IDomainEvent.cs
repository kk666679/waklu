namespace HalalChain.Domain.Common;

/// <summary>
/// Marker for domain events. Events are records, immutable, and named in
/// the past tense. They describe what happened, not what should happen.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
