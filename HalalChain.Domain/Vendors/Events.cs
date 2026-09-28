namespace HalalChain.Domain.Vendors;

using HalalChain.Domain.Common;

public sealed record VendorRegistered(
    VendorId VendorId,
    string LegalName,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public sealed record VendorActivated(
    VendorId VendorId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public sealed record VendorSuspended(
    VendorId VendorId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
