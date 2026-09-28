namespace HalalChain.Application.Halal.Interfaces;

using HalalChain.Domain.Halal;

public interface ICertificateRepository
{
    Task<Certificate?> GetAsync(CertificateId id, CancellationToken ct = default);

    Task<IReadOnlyList<Certificate>> QueryExpiringAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);

    Task<IReadOnlyList<Certificate>> ListForVendorAsync(
        Guid vendorId,
        CancellationToken ct = default);

    Task AddAsync(Certificate certificate, CancellationToken ct = default);
}
