using HalalChain.Domain.Halal;
using HalalChain.Marketplace.Data;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Marketplace.Repositories;

/// <summary>Implementation of ICertificateRepository using EF Core against PlatformDbContext.</summary>
public class CertificateRepository : ICertificateRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<CertificateRepository> _logger;

    public CertificateRepository(PlatformDbContext context, ILogger<CertificateRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Certificate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Certificates
                .Include(c => c.Verifications)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving certificate by ID: {CertificateId}", id);
            throw;
        }
    }

    public async Task<Certificate?> GetByCertificateNumberAsync(string certificateNumber, CancellationToken ct = default)
    {
        try
        {
            return await _context.Certificates
                .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving certificate by number: {CertificateNumber}", certificateNumber);
            throw;
        }
    }

    public async Task<IEnumerable<Certificate>> GetByProductAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            return await _context.Certificates
                .Where(c => c.ProductId == productId)
                .Include(c => c.Verifications)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving certificates for product: {ProductId}", productId);
            throw;
        }
    }

    public async Task<IEnumerable<Certificate>> GetByCertificationBodyAsync(string certificationBody, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Certificates
                .Where(c => c.CertificationBody == certificationBody)
                .OrderByDescending(c => c.IssueDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving certificates by body: {CertificationBody}", certificationBody);
            throw;
        }
    }

    public async Task<IEnumerable<Certificate>> GetByStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Certificates
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving certificates by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<Certificate>> GetExpiringAsync(CancellationToken ct = default)
    {
        try
        {
            var ninetyDaysFromNow = DateTime.UtcNow.AddDays(90);
            return await _context.Certificates
                .Where(c => c.ExpiryDate <= ninetyDaysFromNow && c.ExpiryDate > DateTime.UtcNow)
                .OrderBy(c => c.ExpiryDate)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring certificates");
            throw;
        }
    }

    public async Task<IEnumerable<Certificate>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Certificates
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all certificates");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Certificates.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting certificates");
            throw;
        }
    }

    public async Task AddAsync(Certificate certificate, CancellationToken ct = default)
    {
        try
        {
            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Certificate added: {CertificateId}", certificate.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding certificate: {CertificateId}", certificate.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Certificate certificate, CancellationToken ct = default)
    {
        try
        {
            _context.Certificates.Update(certificate);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Certificate updated: {CertificateId}", certificate.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating certificate: {CertificateId}", certificate.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var certificate = await _context.Certificates.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (certificate != null)
            {
                _context.Certificates.Remove(certificate);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Certificate deleted: {CertificateId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting certificate: {CertificateId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Certificates.AnyAsync(c => c.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking certificate existence: {CertificateId}", id);
            throw;
        }
    }
}
