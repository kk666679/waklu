using HalalChain.Domain.Halal;

namespace HalalChain.Marketplace.Repositories;

/// <summary>
/// Repository for Certificate aggregate root queries and persistence.
/// Manages halal certificates submitted by vendors and their verification lifecycle.
/// </summary>
public interface ICertificateRepository
{
    /// <summary>Get a certificate by ID.</summary>
    Task<Certificate?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get certificate by certificate number.</summary>
    Task<Certificate?> GetByCertificateNumberAsync(string certificateNumber, CancellationToken ct = default);

    /// <summary>Get all certificates for a product.</summary>
    Task<IEnumerable<Certificate>> GetByProductAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Get all certificates from a specific certification body.</summary>
    Task<IEnumerable<Certificate>> GetByCertificationBodyAsync(string certificationBody, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get certificates by status (Submitted, DocumentReview, Verification, Verified, etc.).</summary>
    Task<IEnumerable<Certificate>> GetByStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get certificates expiring soon (within 90 days).</summary>
    Task<IEnumerable<Certificate>> GetExpiringAsync(CancellationToken ct = default);

    /// <summary>Get all certificates (with pagination).</summary>
    Task<IEnumerable<Certificate>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get certificate count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>Save a new certificate.</summary>
    Task AddAsync(Certificate certificate, CancellationToken ct = default);

    /// <summary>Update an existing certificate.</summary>
    Task UpdateAsync(Certificate certificate, CancellationToken ct = default);

    /// <summary>Delete a certificate.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a certificate exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
