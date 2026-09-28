namespace HalalChain.Domain.Halal;

using HalalChain.Domain.Common;

/// <summary>
/// A halal certificate as recorded by the platform.
///
/// The certificate is evidence. It asserts a fact — "this issuer certified
/// this scope until this date" — but it does not assert a verdict. The
/// verdict is a tawheed decision, and it lives on the product as a
/// VerdictBinding, not here.
///
/// Once registered, a certificate is immutable. Revocation is a separate
/// state, not a mutation of the original record.
/// </summary>
public sealed class Certificate : AggregateRoot<CertificateId>
{
    public string CertificateNumber { get; private set; } = string.Empty;
    public string IssuerName { get; private set; } = string.Empty;
    public string IssuerBodyCode { get; private set; } = string.Empty;
    public Guid VendorId { get; private set; }
    public string Scope { get; private set; } = string.Empty;
    public string BlobContentHash { get; private set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? RevocationReason { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }

    private Certificate() { }

    public static Certificate Register(
        Guid vendorId,
        string certificateNumber,
        string issuerName,
        string issuerBodyCode,
        string scope,
        string blobContentHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificateNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobContentHash);

        if (expiresAt <= issuedAt)
            throw new ArgumentException("Expiry must be after issue date.", nameof(expiresAt));

        var certificate = new Certificate
        {
            Id = CertificateId.New(),
            VendorId = vendorId,
            CertificateNumber = certificateNumber,
            IssuerName = issuerName,
            IssuerBodyCode = issuerBodyCode,
            Scope = scope,
            BlobContentHash = blobContentHash,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            RegisteredAt = now,
        };

        certificate.Raise(new CertificateRegistered(
            certificate.Id, vendorId, certificateNumber, expiresAt, now));

        return certificate;
    }

    public bool IsCurrent(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

    public bool IsExpiringSoon(DateTimeOffset now, TimeSpan window) =>
        IsCurrent(now) && ExpiresAt - now <= window;

    public void Revoke(string reason, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (IsRevoked) return;

        IsRevoked = true;
        RevocationReason = reason;
        Raise(new CertificateRevoked(Id, reason, now));
    }
}
