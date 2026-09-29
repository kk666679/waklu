namespace HalalChain.Marketplace.Services.Dashboard.CacheStrategies;

using HalalChain.Marketplace.Models.Dashboard;
using Microsoft.Extensions.Options;

/// <summary>
/// Manager for dashboard configuration caching.
/// Selects and configures cache strategy based on application settings.
/// Handles cache invalidation, warm-up, and monitoring.
/// </summary>
public class DashboardCacheManager : IDashboardCacheManager
{
    private readonly IDashboardCacheStrategy _strategy;
    private readonly DashboardCacheOptions _options;
    private readonly ILogger<DashboardCacheManager> _logger;

    public DashboardCacheManager(
        IDashboardCacheStrategy strategy,
        IOptions<DashboardCacheOptions> options,
        ILogger<DashboardCacheManager> logger)
    {
        _strategy = strategy;
        _options = options.Value;
        _logger = logger;

        _logger.LogInformation(
            "Dashboard cache manager initialized with strategy: {Strategy}",
            strategy.GetBackendName());
    }

    public async Task<TenantDashboardConfig?> GetConfigAsync(string cacheKey)
    {
        try
        {
            var config = await _strategy.GetAsync(cacheKey);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving config from cache: {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task SetConfigAsync(
        string cacheKey,
        TenantDashboardConfig config,
        int cacheDurationSeconds = 0)
    {
        try
        {
            var ttl = cacheDurationSeconds > 0
                ? TimeSpan.FromSeconds(cacheDurationSeconds)
                : TimeSpan.FromSeconds(_options.DefaultCacheDurationSeconds);

            await _strategy.SetAsync(cacheKey, config, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting config in cache: {CacheKey}", cacheKey);
        }
    }

    public async Task InvalidateConfigAsync(string cacheKey)
    {
        try
        {
            await _strategy.RemoveAsync(cacheKey);
            _logger.LogInformation("Cache invalidated: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache: {CacheKey}", cacheKey);
        }
    }

    public async Task InvalidateTenantAsync(string tenantId)
    {
        try
        {
            await _strategy.RemoveByTenantAsync(tenantId);
            _logger.LogInformation("Tenant cache invalidated: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating tenant cache: {TenantId}", tenantId);
        }
    }

    public async Task InvalidateAllAsync()
    {
        try
        {
            if (_options.AllowGlobalInvalidation)
            {
                await _strategy.ClearAllAsync();
                _logger.LogWarning("All cache entries cleared");
            }
            else
            {
                _logger.LogWarning("Global cache invalidation is disabled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache");
        }
    }

    public async Task<bool> IsCachedAsync(string cacheKey)
    {
        try
        {
            return await _strategy.ExistsAsync(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking cache: {CacheKey}", cacheKey);
            return false;
        }
    }

    public async Task<TimeSpan?> GetTtlAsync(string cacheKey)
    {
        try
        {
            return await _strategy.GetTtlAsync(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting TTL: {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task<CacheStrategyStats> GetStatsAsync()
    {
        try
        {
            return await _strategy.GetStatsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStrategyStats { BackendName = "Error retrieving stats" };
        }
    }

    public string GetStrategyName() => _strategy.GetBackendName();
}

/// <summary>Interface for dashboard cache manager.</summary>
public interface IDashboardCacheManager
{
    Task<TenantDashboardConfig?> GetConfigAsync(string cacheKey);
    Task SetConfigAsync(string cacheKey, TenantDashboardConfig config, int cacheDurationSeconds = 0);
    Task InvalidateConfigAsync(string cacheKey);
    Task InvalidateTenantAsync(string tenantId);
    Task InvalidateAllAsync();
    Task<bool> IsCachedAsync(string cacheKey);
    Task<TimeSpan?> GetTtlAsync(string cacheKey);
    Task<CacheStrategyStats> GetStatsAsync();
    string GetStrategyName();
}

/// <summary>Cache manager options.</summary>
public class DashboardCacheOptions
{
    public int DefaultCacheDurationSeconds { get; set; } = 300;
    public bool UseDistributedCache { get; set; } = true;
    public bool UseInMemoryCache { get; set; } = true;
    public bool UseHybridCache { get; set; } = true;
    public bool AllowGlobalInvalidation { get; set; } = true;
    public string RedisConnectionString { get; set; } = "localhost:6379";
}
