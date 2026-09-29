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
