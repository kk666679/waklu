namespace HalalChain.Domain.Commerce;

using HalalChain.Domain.Common;

public sealed record OrderPlaced(
    OrderId OrderId,
    Guid BuyerId,
    decimal TotalAmount,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public sealed record OrderConfirmed(
    OrderId OrderId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
