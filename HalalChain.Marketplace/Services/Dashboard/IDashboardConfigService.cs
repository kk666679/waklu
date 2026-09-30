namespace HalalChain.Marketplace.Services.Dashboard;

using HalalChain.Marketplace.Models.Dashboard;
using HalalChain.Application.Tenancy;

/// <summary>
/// Service for loading, managing, and caching tenant dashboard configurations.
/// Supports multiple storage backends (database, appsettings, runtime overrides).
/// Implements caching with tenant-specific TTLs.
/// </summary>
public interface IDashboardConfigService
{
    /// <summary>
    /// Gets the dashboard configuration for a tenant.
    /// Returns merged configuration from: defaults → appsettings → database → runtime overrides.
    /// Respects cache TTL and tenant-specific cache settings.
    /// </summary>
    Task<TenantDashboardConfig> GetConfigAsync(ITenantContext tenantContext);

    /// <summary>
    /// Gets the dashboard configuration without caching (always fresh from storage).
    /// </summary>
    Task<TenantDashboardConfig> GetConfigAsync(string tenantId, bool bypassCache = false);

    /// <summary>
    /// Updates tenant dashboard configuration in storage.
    /// Invalidates related caches automatically.
    /// </summary>
    Task UpdateConfigAsync(string tenantId, TenantDashboardConfig config);

    /// <summary>
    /// Gets sidebar configuration for a tenant.
    /// </summary>
    Task<SidebarConfig> GetSidebarConfigAsync(ITenantContext tenantContext);

    /// <summary>
    /// Gets header configuration for a tenant.
    /// </summary>
    Task<HeaderConfig> GetHeaderConfigAsync(ITenantContext tenantContext);

    /// <summary>
    /// Gets footer configuration for a tenant.
    /// </summary>
    Task<FooterConfig> GetFooterConfigAsync(ITenantContext tenantContext);

    /// <summary>
    /// Gets navigation items for a tenant, filtered by user capabilities.
    /// </summary>
    Task<List<NavigationItem>> GetNavigationAsync(ITenantContext tenantContext);

    /// <summary>
    /// Gets dashboard region configurations for a tenant.
    /// </summary>
    Task<Dictionary<string, DashboardRegionConfig>> GetRegionsAsync(ITenantContext tenantContext);

    /// <summary>
    /// Gets widgets for a specific dashboard region, filtered by user role and capabilities.
    /// </summary>
    Task<List<WidgetConfig>> GetRegionWidgetsAsync(
        ITenantContext tenantContext,
        string regionId);

    /// <summary>
    /// Gets a specific widget configuration.
    /// </summary>
    Task<WidgetConfig?> GetWidgetConfigAsync(
        ITenantContext tenantContext,
        string widgetId);

    /// <summary>
    /// Gets permissions for a specific role within a tenant.
    /// </summary>
    Task<DashboardRolePermissions?> GetRolePermissionsAsync(
        string tenantId,
        string role);

    /// <summary>
    /// Checks if a feature flag is enabled for a tenant.
    /// </summary>
    Task<bool> IsFeatureEnabledAsync(
        string tenantId,
        string featureFlag);

    /// <summary>
    /// Gets data source URL for a tenant (e.g., API endpoint).
    /// </summary>
    Task<string?> GetDataSourceAsync(
        string tenantId,
        string dataSourceKey);

    /// <summary>
    /// Invalidates all caches for a tenant (called after config updates).
    /// </summary>
    Task InvalidateCacheAsync(string tenantId);

    /// <summary>
    /// Invalidates all caches globally (called during system maintenance).
    /// </summary>
    Task InvalidateAllCachesAsync();

    /// <summary>
    /// Gets cache statistics for monitoring (hit rate, size, TTL).
    /// </summary>
    Task<CacheStatistics> GetCacheStatsAsync();

    /// <summary>
    /// Gets all configuration versions for a tenant (for version history/rollback).
    /// </summary>
    Task<List<ConfigurationVersionInfo>> GetConfigurationVersionsAsync(string tenantId);

    /// <summary>
    /// Activates a specific configuration version.
    /// Automatically deactivates other versions for the tenant.
    /// </summary>
    Task ActivateConfigurationVersionAsync(int configurationId, string? reason = null);

    /// <summary>
    /// Gets audit log entries for a tenant's dashboard configuration changes.
    /// </summary>
    Task<List<ConfigurationAuditEntry>> GetAuditLogAsync(
        string tenantId,
        int? configurationId = null,
        DateTime? since = null,
        int limit = 100);
}

/// <summary>Configuration version information for version history.</summary>
public class ConfigurationVersionInfo
{
    public int Id { get; set; }
    public string TenantId { get; set; } = null!;
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Cache statistics for monitoring.</summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public double HitRate => TotalEntries > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
    public long CacheSizeBytes { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Configuration audit entry for tracking changes.</summary>
public class ConfigurationAuditEntry
{
    public long Id { get; set; }
    public string TenantId { get; set; } = null!;
    public int ConfigurationId { get; set; }
    public string Operation { get; set; } = null!;
    public string? ChangesSummary { get; set; }
    public string ChangedBy { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
    public string? RequestIpAddress { get; set; }
    public string? UserAgent { get; set; }
}
