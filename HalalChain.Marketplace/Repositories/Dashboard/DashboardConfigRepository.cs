namespace HalalChain.Marketplace.Repositories.Dashboard;

using HalalChain.Marketplace.Models.Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

/// <summary>
/// EF Core implementation of IDashboardConfigRepository.
/// Provides CRUD operations and audit trail tracking for dashboard configurations.
/// </summary>
public class DashboardConfigRepository : IDashboardConfigRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardConfigRepository> _logger;

    public DashboardConfigRepository(
        ApplicationDbContext context,
        ILogger<DashboardConfigRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardConfigEntity?> GetActiveConfigAsync(string tenantId)
    {
        var config = await _context.DashboardConfigs
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync();

        _logger.LogDebug("Retrieved active config for tenant {TenantId}: {ConfigId}", tenantId, config?.Id);
        return config;
    }

    public async Task<DashboardConfigEntity?> GetByIdAsync(int configId)
    {
        return await _context.DashboardConfigs.FindAsync(configId);
    }

    public async Task<List<DashboardConfigEntity>> GetAllVersionsAsync(string tenantId)
    {
        return await _context.DashboardConfigs
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<DashboardConfigEntity> CreateAsync(
        string tenantId,
        string configurationJson,
        string createdBy,
        bool replaceActive = true,
        string? notes = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // If replaceActive, deactivate current active config
            if (replaceActive)
            {
                var currentActive = await _context.DashboardConfigs
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.IsActive);

                if (currentActive != null)
                {
                    currentActive.IsActive = false;
                    _context.DashboardConfigs.Update(currentActive);

                    // Record deactivation in audit log
                    var deactivateAudit = new DashboardConfigAuditEntity
                    {
                        TenantId = tenantId,
                        ConfigurationId = currentActive.Id,
                        Operation = "Deactivate",
                        PreviousConfigurationJson = currentActive.ConfigurationJson,
                        ChangedBy = createdBy,
                        ChangedAt = DateTime.UtcNow,
                        Reason = "Replaced by new configuration"
                    };
                    _context.DashboardConfigAudits.Add(deactivateAudit);
                }
            }

            // Create new configuration
            var entity = new DashboardConfigEntity
            {
                TenantId = tenantId,
                ConfigurationJson = configurationJson,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
                IsActive = true,
                Version = 1,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DashboardConfigs.Add(entity);

            // Record creation in audit log
            var createAudit = new DashboardConfigAuditEntity
            {
                TenantId = tenantId,
                ConfigurationId = entity.Id,
                Operation = "Create",
                NewConfigurationJson = configurationJson,
                ChangedBy = createdBy,
                ChangedAt = DateTime.UtcNow,
                ChangesSummary = "Initial configuration created"
            };
            _context.DashboardConfigAudits.Add(createAudit);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Created dashboard configuration for tenant {TenantId}: ConfigId={ConfigId}",
                tenantId, entity.Id);

            return entity;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating configuration for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<DashboardConfigEntity> UpdateAsync(
        DashboardConfigEntity entity,
        string updatedBy,
        string? reason = null)
    {
        try
        {
            // Get previous version for audit
            var previousEntity = await _context.DashboardConfigs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == entity.Id);

            if (previousEntity == null)
                throw new InvalidOperationException($"Configuration {entity.Id} not found");

            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.Version = previousEntity.Version + 1;

            _context.DashboardConfigs.Update(entity);

            // Record update in audit log
            var audit = new DashboardConfigAuditEntity
            {
                TenantId = entity.TenantId,
                ConfigurationId = entity.Id,
                Operation = "Update",
                PreviousConfigurationJson = previousEntity.ConfigurationJson,
                NewConfigurationJson = entity.ConfigurationJson,
                ChangedBy = updatedBy,
                ChangedAt = DateTime.UtcNow,
                Reason = reason,
                ChangesSummary = "Configuration updated"
            };
            _context.DashboardConfigAudits.Add(audit);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated dashboard configuration: TenantId={TenantId}, ConfigId={ConfigId}, Version={Version}",
                entity.TenantId, entity.Id, entity.Version);

            return entity;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict updating configuration {ConfigId}", entity.Id);
            throw new ConcurrencyException("Configuration was modified concurrently. Please refresh and try again.", ex);
        }
    }

    public async Task<DashboardConfigEntity> ActivateAsync(
        int configId,
        string activatedBy,
        string? reason = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var config = await _context.DashboardConfigs.FindAsync(configId);
            if (config == null)
                throw new InvalidOperationException($"Configuration {configId} not found");

            // Deactivate other versions for this tenant
            var otherActive = await _context.DashboardConfigs
                .Where(c => c.TenantId == config.TenantId && c.Id != configId && c.IsActive)
                .ToListAsync();

            foreach (var other in otherActive)
            {
                other.IsActive = false;
            }

            config.IsActive = true;
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedBy = activatedBy;

            _context.DashboardConfigs.UpdateRange(otherActive);
            _context.DashboardConfigs.Update(config);

            // Record activation
            var audit = new DashboardConfigAuditEntity
            {
                TenantId = config.TenantId,
                ConfigurationId = configId,
                Operation = "Activate",
                NewConfigurationJson = config.ConfigurationJson,
                ChangedBy = activatedBy,
                ChangedAt = DateTime.UtcNow,
                Reason = reason ?? "Configuration activated"
            };
            _context.DashboardConfigAudits.Add(audit);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Activated dashboard configuration: ConfigId={ConfigId}, TenantId={TenantId}",
                configId, config.TenantId);

            return config;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error activating configuration {ConfigId}", configId);
            throw;
        }
    }

    public async Task DeactivateAsync(int configId, string deactivatedBy, string? reason = null)
    {
        var config = await _context.DashboardConfigs.FindAsync(configId);
        if (config == null)
            throw new InvalidOperationException($"Configuration {configId} not found");

        config.IsActive = false;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = deactivatedBy;

        _context.DashboardConfigs.Update(config);

        var audit = new DashboardConfigAuditEntity
        {
            TenantId = config.TenantId,
            ConfigurationId = configId,
            Operation = "Deactivate",
            ChangedBy = deactivatedBy,
            ChangedAt = DateTime.UtcNow,
            Reason = reason ?? "Configuration deactivated"
        };
        _context.DashboardConfigAudits.Add(audit);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Deactivated configuration {ConfigId}", configId);
    }

    public async Task DeleteAsync(int configId, string deletedBy, string? reason = null)
    {
        var config = await _context.DashboardConfigs.FindAsync(configId);
        if (config == null)
            throw new InvalidOperationException($"Configuration {configId} not found");

        // Soft delete
        config.IsActive = false;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = deletedBy;

        _context.DashboardConfigs.Update(config);

        var audit = new DashboardConfigAuditEntity
        {
            TenantId = config.TenantId,
            ConfigurationId = configId,
            Operation = "Delete",
            ChangedBy = deletedBy,
            ChangedAt = DateTime.UtcNow,
            Reason = reason ?? "Configuration deleted"
        };
        _context.DashboardConfigAudits.Add(audit);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted configuration {ConfigId}", configId);
    }

    public async Task<List<DashboardConfigAuditEntity>> GetAuditLogAsync(
        string tenantId,
        int? configId = null,
        DateTime? since = null,
        int limit = 100)
    {
        var query = _context.DashboardConfigAudits
            .Where(a => a.TenantId == tenantId);

        if (configId.HasValue)
            query = query.Where(a => a.ConfigurationId == configId);

        if (since.HasValue)
            query = query.Where(a => a.ChangedAt >= since.Value);

        return await query
            .OrderByDescending(a => a.ChangedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<(List<DashboardConfigEntity>, int)> SearchAsync(
        string? tenantId = null,
        bool? isActive = null,
        DateTime? createdAfter = null,
        DateTime? createdBefore = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _context.DashboardConfigs.AsQueryable();

        if (!string.IsNullOrEmpty(tenantId))
            query = query.Where(c => c.TenantId == tenantId);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (createdAfter.HasValue)
            query = query.Where(c => c.CreatedAt >= createdAfter.Value);

        if (createdBefore.HasValue)
            query = query.Where(c => c.CreatedAt <= createdBefore.Value);

        var totalCount = await query.CountAsync();

        var configs = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (configs, totalCount);
    }

    public async Task<int> GetActiveTenantCountAsync()
    {
        return await _context.DashboardConfigs
            .Where(c => c.IsActive)
            .Select(c => c.TenantId)
            .Distinct()
            .CountAsync();
    }

    public async Task<bool> HasBeenModifiedSinceAsync(string tenantId, DateTime since)
    {
        return await _context.DashboardConfigs
            .Where(c => c.TenantId == tenantId && c.UpdatedAt > since)
            .AnyAsync();
    }

    public async Task<Dictionary<string, DashboardConfigEntity>> GetBatchAsync(List<string> tenantIds)
    {
        var configs = await _context.DashboardConfigs
            .Where(c => tenantIds.Contains(c.TenantId) && c.IsActive)
            .ToListAsync();

        return configs.ToDictionary(c => c.TenantId);
    }
}
