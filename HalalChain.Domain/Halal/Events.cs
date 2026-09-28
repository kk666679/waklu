namespace HalalChain.Domain.Halal;

using HalalChain.Domain.Catalog;
using HalalChain.Domain.Common;

public sealed record CertificateRegistered(
    CertificateId CertificateId,
    Guid VendorId,
    string CertificateNumber,
    DateTimeOffset ExpiresAt,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public sealed record CertificateRevoked(
    CertificateId CertificateId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
