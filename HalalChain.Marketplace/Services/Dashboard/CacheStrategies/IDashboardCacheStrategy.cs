namespace HalalChain.Marketplace.Services.Dashboard.CacheStrategies;

using HalalChain.Marketplace.Models.Dashboard;

/// <summary>
/// Abstract cache strategy for dashboard configurations.
/// Supports multiple backends: Redis (distributed), in-memory (local), hybrid (both).
/// </summary>
public interface IDashboardCacheStrategy
{
    /// <summary>Gets a configuration from cache.</summary>
    Task<TenantDashboardConfig?> GetAsync(string cacheKey);

    /// <summary>Sets a configuration in cache with optional TTL.</summary>
    Task SetAsync(string cacheKey, TenantDashboardConfig config, TimeSpan? expiration = null);

    /// <summary>Removes a configuration from cache.</summary>
    Task RemoveAsync(string cacheKey);

    /// <summary>Removes all configurations for a tenant from cache.</summary>
    Task RemoveByTenantAsync(string tenantId);

    /// <summary>Checks if a key exists in cache.</summary>
    Task<bool> ExistsAsync(string cacheKey);

    /// <summary>Gets time-to-live for a cache entry.</summary>
    Task<TimeSpan?> GetTtlAsync(string cacheKey);

    /// <summary>Clears all cache entries.</summary>
    Task ClearAllAsync();

    /// <summary>Gets cache statistics (hits, misses, size).</summary>
    Task<CacheStrategyStats> GetStatsAsync();

    /// <summary>Gets backend name (e.g., "Redis", "Memory", "Hybrid").</summary>
    string GetBackendName();
}

/// <summary>Cache strategy statistics.</summary>
public class CacheStrategyStats
{
    public string BackendName { get; set; } = null!;
    public int TotalEntries { get; set; }
    public long CacheSizeBytes { get; set; }
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public double HitRate => TotalEntries > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}
