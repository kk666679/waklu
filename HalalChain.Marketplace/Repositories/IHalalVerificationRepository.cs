using HalalChain.Domain.Halal;

namespace HalalChain.Marketplace.Repositories;

/// <summary>
/// Repository for HalalVerification aggregate root queries and persistence.
/// Manages deterministic compliance results from the Policy Engine.
/// </summary>
public interface IHalalVerificationRepository
{
    /// <summary>Get a verification by ID.</summary>
    Task<HalalVerification?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get verification for a product (should be one per product, unique).</summary>
    Task<HalalVerification?> GetByProductAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Get verifications by compliance status (Verified, ManualReview, Incomplete, Hold, NonCompliant, Unverified).</summary>
    Task<IEnumerable<HalalVerification>> GetByComplianceStatusAsync(ComplianceStatus status, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get verifications requiring human review.</summary>
    Task<IEnumerable<HalalVerification>> GetRequiringReviewAsync(int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get verifications by jurisdiction (for regional compliance).</summary>
    Task<IEnumerable<HalalVerification>> GetByJurisdictionAsync(string jurisdiction, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get verifications by policy version (for tracking policy changes).</summary>
    Task<IEnumerable<HalalVerification>> GetByPolicyVersionAsync(string policyVersion, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get recently verified products.</summary>
    Task<IEnumerable<HalalVerification>> GetRecentAsync(int take = 20, CancellationToken ct = default);

    /// <summary>Get all verifications (with pagination).</summary>
    Task<IEnumerable<HalalVerification>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get verification count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>Get evidence items for a verification.</summary>
    Task<IEnumerable<VerificationEvidence>> GetEvidenceByVerificationAsync(Guid verificationId, CancellationToken ct = default);

    /// <summary>Get audit trail events for a verification.</summary>
    Task<IEnumerable<VerificationAudit>> GetAuditTrailAsync(Guid verificationId, CancellationToken ct = default);

    /// <summary>Save a new verification.</summary>
    Task AddAsync(HalalVerification verification, CancellationToken ct = default);

    /// <summary>Update an existing verification.</summary>
    Task UpdateAsync(HalalVerification verification, CancellationToken ct = default);

    /// <summary>Delete a verification.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a verification exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
