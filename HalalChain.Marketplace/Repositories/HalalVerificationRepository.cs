using HalalChain.Domain.Halal;
using HalalChain.Marketplace.Data;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Marketplace.Repositories;

/// <summary>Implementation of IHalalVerificationRepository using EF Core against PlatformDbContext.</summary>
public class HalalVerificationRepository : IHalalVerificationRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<HalalVerificationRepository> _logger;

    public HalalVerificationRepository(PlatformDbContext context, ILogger<HalalVerificationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HalalVerification?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.HalalVerifications
                .Include(v => v.Evidences)
                .Include(v => v.Audits)
                .FirstOrDefaultAsync(v => v.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verification by ID: {VerificationId}", id);
            throw;
        }
    }

    public async Task<HalalVerification?> GetByProductAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            return await _context.HalalVerifications
                .Include(v => v.Evidences)
                .Include(v => v.Audits)
                .FirstOrDefaultAsync(v => v.ProductId == productId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verification for product: {ProductId}", productId);
            throw;
        }
    }

    public async Task<IEnumerable<HalalVerification>> GetByComplianceStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.HalalVerifications
                .Where(v => v.ComplianceStatus == status)
                .Include(v => v.Evidences)
                .OrderByDescending(v => v.VerifiedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verifications by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<HalalVerification>> GetRequiringReviewAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.HalalVerifications
                .Where(v => v.RequiresHumanReview)
                .Include(v => v.Evidences)
                .OrderByDescending(v => v.VerifiedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verifications requiring review");
            throw;
        }
    }

    public async Task<IEnumerable<HalalVerification>> GetByJurisdictionAsync(string jurisdiction, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.HalalVerifications
                .Where(v => v.Jurisdiction == jurisdiction)
                .Include(v => v.Evidences)
                .OrderByDescending(v => v.VerifiedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verifications by jurisdiction: {Jurisdiction}", jurisdiction);
            throw;
        }
    }

    public async Task<IEnumerable<HalalVerification>> GetByPolicyVersionAsync(string policyVersion, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.HalalVerifications
                .Where(v => v.PolicyVersion == policyVersion)
                .Include(v => v.Evidences)
                .OrderByDescending(v => v.VerifiedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verifications by policy version: {PolicyVersion}", policyVersion);
            throw;
        }
    }

    public async Task<IEnumerable<HalalVerification>> GetRecentAsync(int take = 20, CancellationToken ct = default)
    {
        try
        {
            return await _context.HalalVerifications
                .Include(v => v.Evidences)
                .OrderByDescending(v => v.VerifiedAt)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent verifications");
            throw;
        }
    }

    public async Task<IEnumerable<HalalVerification>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.HalalVerifications
                .Include(v => v.Evidences)
                .OrderByDescending(v => v.VerifiedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all verifications");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.HalalVerifications.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting verifications");
            throw;
        }
    }

    public async Task<IEnumerable<VerificationEvidence>> GetEvidenceByVerificationAsync(Guid verificationId, CancellationToken ct = default)
    {
        try
        {
            return await _context.VerificationEvidences
                .Where(e => e.VerificationId == verificationId)
                .OrderByDescending(e => e.CollectedAt)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidence for verification: {VerificationId}", verificationId);
            throw;
        }
    }

    public async Task<IEnumerable<VerificationAudit>> GetAuditTrailAsync(Guid verificationId, CancellationToken ct = default)
    {
        try
        {
            return await _context.VerificationAudits
                .Where(a => a.VerificationId == verificationId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for verification: {VerificationId}", verificationId);
            throw;
        }
    }

    public async Task AddAsync(HalalVerification verification, CancellationToken ct = default)
    {
        try
        {
            _context.HalalVerifications.Add(verification);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Verification added: {VerificationId}", verification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding verification: {VerificationId}", verification.Id);
            throw;
        }
    }

    public async Task UpdateAsync(HalalVerification verification, CancellationToken ct = default)
    {
        try
        {
            _context.HalalVerifications.Update(verification);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Verification updated: {VerificationId}", verification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating verification: {VerificationId}", verification.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var verification = await _context.HalalVerifications.FirstOrDefaultAsync(v => v.Id == id, ct);
            if (verification != null)
            {
                _context.HalalVerifications.Remove(verification);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Verification deleted: {VerificationId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting verification: {VerificationId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.HalalVerifications.AnyAsync(v => v.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking verification existence: {VerificationId}", id);
            throw;
        }
    }
}
