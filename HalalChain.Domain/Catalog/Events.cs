namespace HalalChain.Domain.Catalog;

using HalalChain.Domain.Common;

public sealed record ProductCreated(
    ProductId ProductId,
    Guid VendorId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public sealed record VerdictBound(
    ProductId ProductId,
    VerdictState State,
    string PolicyVersion,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public sealed record ProductStatusChanged(
    ProductId ProductId,
    ProductStatus From,
    ProductStatus To,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
