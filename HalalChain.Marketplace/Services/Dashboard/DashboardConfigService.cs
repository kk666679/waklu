namespace HalalChain.Marketplace.Services.Dashboard;

using HalalChain.Marketplace.Models.Dashboard;
using HalalChain.Application.Tenancy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

/// <summary>
/// Implementation of IDashboardConfigService.
/// 
/// Configuration resolution hierarchy:
/// 1. Runtime overrides (in-memory, highest priority)
/// 2. Database configuration (tenant-specific customizations)
/// 3. appsettings.json configuration (environment-level defaults)
/// 4. Hard-coded defaults (lowest priority)
/// 
/// Caching strategy:
/// - Use distributed cache (Redis) for configs
/// - Fall back to in-memory cache if Redis unavailable
/// - Respect tenant-specific cache TTL
/// - Invalidate on config updates
/// </summary>
public class DashboardConfigService : IDashboardConfigService
{
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly DashboardConfigOptions _options;
    private readonly ILogger<DashboardConfigService> _logger;

    // In-memory cache for runtime overrides and fallback
    private readonly ConcurrentDictionary<string, (TenantDashboardConfig config, DateTime expiry)> _runtimeCache = new();

    // Cache statistics
    private readonly ConcurrentDictionary<string, int> _cacheHits = new();
    private readonly ConcurrentDictionary<string, int> _cacheMisses = new();

    public DashboardConfigService(
        IDistributedCache cache,
        IConfiguration configuration,
        IOptions<DashboardConfigOptions> options,
        ILogger<DashboardConfigService> logger)
    {
        _cache = cache;
        _configuration = configuration;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TenantDashboardConfig> GetConfigAsync(ITenantContext tenantContext)
    {
        return await GetConfigAsync(tenantContext.TenantId, bypassCache: false);
    }

    public async Task<TenantDashboardConfig> GetConfigAsync(string tenantId, bool bypassCache = false)
    {
        var cacheKey = $"dashboard:config:{tenantId}";

        // Check runtime overrides first
        if (!bypassCache && _runtimeCache.TryGetValue(cacheKey, out var runtimeEntry))
        {
            if (DateTime.UtcNow < runtimeEntry.expiry)
            {
                _logger.LogDebug("Dashboard config retrieved from runtime cache for tenant {TenantId}", tenantId);
                RecordCacheHit(cacheKey);
                return runtimeEntry.config;
            }
            else
            {
                _runtimeCache.TryRemove(cacheKey, out _);
            }
        }

        // Check distributed cache
        if (!bypassCache)
        {
            try
            {
                var cached = await _cache.GetAsAsync<TenantDashboardConfig>(cacheKey);
                if (cached != null)
                {
                    _logger.LogDebug("Dashboard config retrieved from distributed cache for tenant {TenantId}", tenantId);
                    RecordCacheHit(cacheKey);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading from distributed cache; falling back to storage");
            }
        }

        RecordCacheMiss(cacheKey);

        // Load from storage (merged from all sources)
        var config = await LoadConfigFromStorageAsync(tenantId);

        // Cache it
        try
        {
            var cacheDuration = config.CacheDurationSeconds > 0
                ? TimeSpan.FromSeconds(config.CacheDurationSeconds)
                : TimeSpan.FromSeconds(_options.DefaultCacheDurationSeconds);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheDuration
            };

            await _cache.SetAsAsync(cacheKey, config, cacheOptions);
            _logger.LogInformation(
                "Dashboard config cached for tenant {TenantId} with TTL {CacheDuration}s",
                tenantId, cacheDuration.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error writing to distributed cache; using in-memory fallback");
            // Fall back to in-memory cache
            var expiry = DateTime.UtcNow.AddSeconds(
                config.CacheDurationSeconds > 0 ? config.CacheDurationSeconds : _options.DefaultCacheDurationSeconds);
            _runtimeCache[cacheKey] = (config, expiry);
        }

        return config;
    }

    public async Task UpdateConfigAsync(string tenantId, TenantDashboardConfig config)
    {
        config.TenantId = tenantId;
        config.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Updating dashboard configuration for tenant {TenantId}", tenantId);

        // Save to storage (database)
        await SaveConfigToStorageAsync(config);

        // Invalidate caches
        await InvalidateCacheAsync(tenantId);

        _logger.LogInformation("Dashboard configuration updated for tenant {TenantId}", tenantId);
    }

    public async Task<SidebarConfig> GetSidebarConfigAsync(ITenantContext tenantContext)
    {
        var config = await GetConfigAsync(tenantContext);
        return config.Sidebar;
    }

    public async Task<HeaderConfig> GetHeaderConfigAsync(ITenantContext tenantContext)
    {
        var config = await GetConfigAsync(tenantContext);
        return config.Header;
    }

    public async Task<FooterConfig> GetFooterConfigAsync(ITenantContext tenantContext)
    {
        var config = await GetConfigAsync(tenantContext);
        return config.Footer;
    }

    public async Task<List<NavigationItem>> GetNavigationAsync(ITenantContext tenantContext)
    {
        var config = await GetConfigAsync(tenantContext);
        
        // Filter navigation items by user capabilities and feature flags
        var filtered = FilterNavigationByCapabilities(config.NavigationItems, tenantContext.Capabilities, config.FeatureFlags);
        
        return filtered.OrderBy(x => x.Order).ToList();
    }

    public async Task<Dictionary<string, DashboardRegionConfig>> GetRegionsAsync(ITenantContext tenantContext)
    {
        var config = await GetConfigAsync(tenantContext);
        return config.Regions;
    }

    public async Task<List<WidgetConfig>> GetRegionWidgetsAsync(
        ITenantContext tenantContext,
        string regionId)
    {
        var config = await GetConfigAsync(tenantContext);

        if (!config.Regions.TryGetValue(regionId, out var region))
        {
            _logger.LogWarning("Dashboard region not found: {RegionId} for tenant {TenantId}", regionId, tenantContext.TenantId);
            return new();
        }

        // Filter widgets by user role and capabilities
        var filtered = region.DefaultWidgets
            .Where(w => w.Visible && HasRequiredCapabilities(w.RequiredCapabilities, tenantContext.Capabilities))
            .OrderBy(w => w.Order)
            .ToList();

        return filtered;
    }

    public async Task<WidgetConfig?> GetWidgetConfigAsync(
        ITenantContext tenantContext,
        string widgetId)
    {
        var config = await GetConfigAsync(tenantContext);

        foreach (var region in config.Regions.Values)
        {
            var widget = region.DefaultWidgets.FirstOrDefault(w => w.Id == widgetId);
            if (widget != null && HasRequiredCapabilities(widget.RequiredCapabilities, tenantContext.Capabilities))
            {
                return widget;
            }
        }

        return null;
    }

    public async Task<DashboardRolePermissions?> GetRolePermissionsAsync(
        string tenantId,
        string role)
    {
        var config = await GetConfigAsync(tenantId);
        return config.RolePermissions.TryGetValue(role, out var permissions) ? permissions : null;
    }

    public async Task<bool> IsFeatureEnabledAsync(
        string tenantId,
        string featureFlag)
    {
        var config = await GetConfigAsync(tenantId);
        return config.FeatureFlags.TryGetValue(featureFlag, out var enabled) && enabled;
    }

    public async Task<string?> GetDataSourceAsync(
        string tenantId,
        string dataSourceKey)
    {
        var config = await GetConfigAsync(tenantId);
        return config.DataSources.TryGetValue(dataSourceKey, out var url) ? url : null;
    }

    public async Task InvalidateCacheAsync(string tenantId)
    {
        var cacheKey = $"dashboard:config:{tenantId}";

        // Remove from runtime cache
        _runtimeCache.TryRemove(cacheKey, out _);

        // Remove from distributed cache
        try
        {
            await _cache.RemoveAsync(cacheKey);
            _logger.LogInformation("Dashboard configuration cache invalidated for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating distributed cache for tenant {TenantId}", tenantId);
        }
    }

    public async Task InvalidateAllCachesAsync()
    {
        _logger.LogWarning("Invalidating all dashboard configuration caches");
        _runtimeCache.Clear();
        // Note: Distributed cache invalidation would require scanning all keys (Redis KEYS command)
        // This is typically done via a separate maintenance task
        await Task.CompletedTask;
    }

    public async Task<CacheStatistics> GetCacheStatsAsync()
    {
        var totalHits = _cacheHits.Values.Sum();
        var totalMisses = _cacheMisses.Values.Sum();

        return await Task.FromResult(new CacheStatistics
        {
            TotalEntries = _runtimeCache.Count,
            HitCount = totalHits,
            MissCount = totalMisses,
            CacheSizeBytes = EstimateCacheSize()
        });
    }

    // Private helper methods

    private async Task<TenantDashboardConfig> LoadConfigFromStorageAsync(string tenantId)
    {
        var config = new TenantDashboardConfig { TenantId = tenantId };

        // 1. Start with hard-coded defaults
        ApplyDefaults(config);

        // 2. Merge appsettings.json configuration
        ApplyConfigurationDefaults(config);

        // 3. Merge tenant-specific configuration from database
        // TODO: Implement database loading
        // var dbConfig = await _configRepository.GetByTenantIdAsync(tenantId);
        // if (dbConfig != null) MergeConfigurations(config, dbConfig);

        _logger.LogInformation("Dashboard configuration loaded for tenant {TenantId}", tenantId);
        return await Task.FromResult(config);
    }

    private async Task SaveConfigToStorageAsync(TenantDashboardConfig config)
    {
        // TODO: Implement database saving
        // await _configRepository.UpdateAsync(config);
        await Task.CompletedTask;
    }

    private void ApplyDefaults(TenantDashboardConfig config)
    {
        // Set organization name if not already set
        if (string.IsNullOrEmpty(config.OrganizationName))
            config.OrganizationName = $"Tenant {config.TenantId}";

        // Initialize sidebar if empty
        if (config.Sidebar == null)
            config.Sidebar = new SidebarConfig();

        // Initialize header if empty
        if (config.Header == null)
            config.Header = new HeaderConfig();

        // Initialize footer if empty
        if (config.Footer == null)
            config.Footer = new FooterConfig();

        // Set default regions
        if (config.Regions.Count == 0)
        {
            config.Regions["main"] = new DashboardRegionConfig
            {
                Id = "main",
                DisplayName = "Main Content",
                LayoutClass = "region-main-grid",
                ComponentName = "DashboardMainRegion"
            };

            config.Regions["sidebar"] = new DashboardRegionConfig
            {
                Id = "sidebar",
                DisplayName = "Sidebar",
                LayoutClass = "region-sidebar",
                ComponentName = "DashboardSidebar"
            };
        }

        // Set default navigation items
        if (config.NavigationItems.Count == 0)
        {
            config.NavigationItems = new List<NavigationItem>
            {
                new NavigationItem
                {
                    Id = "dashboard",
                    Label = "Dashboard",
                    Href = "/dashboard",
                    IconClass = "icon-dashboard",
                    Order = 1
                }
            };
        }

        // Set default permissions for common roles
        if (config.RolePermissions.Count == 0)
        {
            config.RolePermissions["Administrator"] = new DashboardRolePermissions
            {
                Role = "Administrator",
                AccessiblePages = new() { "admin", "vendor", "audit", "compliance" },
                AccessibleWidgets = new() { "*" },
                EditableWidgets = new() { "*" },
                CanPersonalize = true,
                CanExport = true,
                CanAccessAuditLogs = true
            };

            config.RolePermissions["Vendor"] = new DashboardRolePermissions
            {
                Role = "Vendor",
                AccessiblePages = new() { "vendor" },
                AccessibleWidgets = new() { "sales", "orders", "inventory", "reviews" },
                EditableWidgets = new() { },
                CanPersonalize = true,
                CanExport = false,
                CanAccessAuditLogs = false
            };
        }
    }

    private void ApplyConfigurationDefaults(TenantDashboardConfig config)
    {
        // Load from appsettings.json "Dashboard" section
        var dashboardSection = _configuration.GetSection("Dashboard");
        if (dashboardSection.Exists())
        {
            // Override with appsettings values
            if (!string.IsNullOrEmpty(dashboardSection["PrimaryColor"]))
                config.PrimaryColor = dashboardSection["PrimaryColor"]!;

            if (!string.IsNullOrEmpty(dashboardSection["Theme"]))
                config.Theme = dashboardSection["Theme"]!;

            if (int.TryParse(dashboardSection["CacheDurationSeconds"], out var cacheDuration))
                config.CacheDurationSeconds = cacheDuration;
        }
    }

    private List<NavigationItem> FilterNavigationByCapabilities(
        List<NavigationItem> items,
        IReadOnlySet<string> capabilities,
        Dictionary<string, bool> featureFlags)
    {
        return items
            .Where(item =>
            {
                // Check feature flag
                if (!string.IsNullOrEmpty(item.FeatureFlag))
                {
                    if (!featureFlags.TryGetValue(item.FeatureFlag, out var enabled) || !enabled)
                        return false;
                }

                // Check capabilities
                if (item.RequiredCapabilities.Count > 0)
                {
                    return HasRequiredCapabilities(item.RequiredCapabilities, capabilities);
                }

                return true;
            })
            .Select(item => new NavigationItem
            {
                Id = item.Id,
                Label = item.Label,
                Href = item.Href,
                IconClass = item.IconClass,
                Order = item.Order,
                RequiredCapabilities = item.RequiredCapabilities,
                FeatureFlag = item.FeatureFlag,
                Children = FilterNavigationByCapabilities(item.Children, capabilities, featureFlags)
            })
            .ToList();
    }

    private bool HasRequiredCapabilities(List<string> required, IReadOnlySet<string> available)
    {
        if (required.Count == 0) return true;
        return required.All(cap => available.Contains(cap));
    }

    private void RecordCacheHit(string key)
    {
        _cacheHits.AddOrUpdate(key, 1, (_, count) => count + 1);
    }

    private void RecordCacheMiss(string key)
    {
        _cacheMisses.AddOrUpdate(key, 1, (_, count) => count + 1);
    }

    private long EstimateCacheSize()
    {
        return _runtimeCache.Values.Sum(entry =>
            JsonSerializer.Serialize(entry.config).Length * sizeof(char));
    }
}

/// <summary>Configuration options for dashboard config service.</summary>
public class DashboardConfigOptions
{
    public int DefaultCacheDurationSeconds { get; set; } = 300;
    public bool EnableDistributedCache { get; set; } = true;
    public bool EnableRuntimeCache { get; set; } = true;
    public string? ConfigurationStoragePath { get; set; }
}
