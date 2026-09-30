namespace HalalChain.Marketplace.Repositories.Dashboard;

using HalalChain.Marketplace.Models.Dashboard;

/// <summary>
/// Repository interface for persisting and retrieving dashboard configurations.
/// Abstracts storage implementation (EF Core, Dapper, etc).
/// Provides tenant-scoped queries with automatic tenant filtering.
/// </summary>
public interface IDashboardConfigRepository
{
    /// <summary>
    /// Gets active dashboard configuration for a tenant.
    /// Returns null if no active configuration exists.
    /// </summary>
    Task<DashboardConfigEntity?> GetActiveConfigAsync(string tenantId);

    /// <summary>
    /// Gets specific configuration version by ID.
    /// </summary>
    Task<DashboardConfigEntity?> GetByIdAsync(int configId);

    /// <summary>
    /// Gets all configuration versions for a tenant (for version history).
    /// </summary>
    Task<List<DashboardConfigEntity>> GetAllVersionsAsync(string tenantId);

    /// <summary>
    /// Creates a new configuration version.
    /// Automatically marks previous versions as inactive if replaceActive = true.
    /// </summary>
    Task<DashboardConfigEntity> CreateAsync(
        string tenantId,
        string configurationJson,
        string createdBy,
        bool replaceActive = true,
        string? notes = null);

    /// <summary>
    /// Updates an existing configuration (optimistic concurrency control).
    /// Throws ConcurrencyException if version mismatch.
    /// </summary>
    Task<DashboardConfigEntity> UpdateAsync(
        DashboardConfigEntity entity,
        string updatedBy,
        string? reason = null);

    /// <summary>
    /// Activates a specific configuration version.
    /// Automatically deactivates other versions for the tenant.
    /// </summary>
    Task<DashboardConfigEntity> ActivateAsync(
        int configId,
        string activatedBy,
        string? reason = null);

    /// <summary>
    /// Deactivates a configuration version.
    /// </summary>
    Task DeactivateAsync(int configId, string deactivatedBy, string? reason = null);

    /// <summary>
    /// Deletes a configuration version (soft delete).
    /// </summary>
    Task DeleteAsync(int configId, string deletedBy, string? reason = null);

    /// <summary>
    /// Gets audit log entries for a configuration.
    /// </summary>
    Task<List<DashboardConfigAuditEntity>> GetAuditLogAsync(
        string tenantId,
        int? configId = null,
        DateTime? since = null,
        int limit = 100);

    /// <summary>
    /// Gets configurations matching search criteria.
    /// </summary>
    Task<(List<DashboardConfigEntity> configs, int totalCount)> SearchAsync(
        string? tenantId = null,
        bool? isActive = null,
        DateTime? createdAfter = null,
        DateTime? createdBefore = null,
        int pageNumber = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets count of active configurations (for billing/metering).
    /// </summary>
    Task<int> GetActiveTenantCountAsync();

    /// <summary>
    /// Checks if configuration has been modified since a given timestamp.
    /// Used for smart cache invalidation.
    /// </summary>
    Task<bool> HasBeenModifiedSinceAsync(string tenantId, DateTime since);

    /// <summary>
    /// Batch retrieves configurations for multiple tenants.
    /// Useful for bulk operations and cache warm-up.
    /// </summary>
    Task<Dictionary<string, DashboardConfigEntity>> GetBatchAsync(List<string> tenantIds);
}

/// <summary>Exception thrown when optimistic concurrency check fails.</summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException) { }
}
