namespace HalalChain.Marketplace.Services.Dashboard.CacheStrategies;

using HalalChain.Marketplace.Models.Dashboard;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Hybrid cache strategy combining distributed (Redis) and in-memory caches.
/// Provides multi-level caching:
/// 1. Check in-memory cache (L1)
/// 2. Check distributed cache (L2)
/// 3. Load from storage (L3)
/// Updates both caches on retrieval.
/// </summary>
public class HybridCacheStrategy : IDashboardCacheStrategy
{
    private readonly IDashboardCacheStrategy _l1Cache; // In-memory
    private readonly IDashboardCacheStrategy _l2Cache; // Distributed
    private readonly ILogger<HybridCacheStrategy> _logger;
    private int _l1Hits;
    private int _l2Hits;
    private int _misses;

    public HybridCacheStrategy(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<HybridCacheStrategy> logger,
        ILogger<InMemoryCacheStrategy> l1Logger,
        ILogger<DistributedCacheStrategy> l2Logger)
    {
        _l1Cache = new InMemoryCacheStrategy(memoryCache, l1Logger);
        _l2Cache = new DistributedCacheStrategy(distributedCache, l2Logger);
        _logger = logger;
    }

    public async Task<TenantDashboardConfig?> GetAsync(string cacheKey)
    {
        try
        {
            // Check L1 cache (in-memory)
            var config = await _l1Cache.GetAsync(cacheKey);
            if (config != null)
            {
                _l1Hits++;
                _logger.LogDebug("Hybrid cache L1 hit: {CacheKey}", cacheKey);
                return config;
            }

            // Check L2 cache (distributed/Redis)
            config = await _l2Cache.GetAsync(cacheKey);
            if (config != null)
            {
                _l2Hits++;
                // Populate L1 cache for future hits
                await _l1Cache.SetAsync(cacheKey, config);
                _logger.LogDebug("Hybrid cache L2 hit: {CacheKey}", cacheKey);
                return config;
            }

            // Cache miss
            _misses++;
            _logger.LogDebug("Hybrid cache miss: {CacheKey}", cacheKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in hybrid cache get: {CacheKey}", cacheKey);
            _misses++;
            return null;
        }
    }

    public async Task SetAsync(string cacheKey, TenantDashboardConfig config, TimeSpan? expiration = null)
    {
        try
        {
            // Set both caches
            await Task.WhenAll(
                _l1Cache.SetAsync(cacheKey, config, expiration),
                _l2Cache.SetAsync(cacheKey, config, expiration)
            );

            _logger.LogDebug(
                "Hybrid cache set (both L1 and L2): {CacheKey}, TTL={TTL}s",
                cacheKey, expiration?.TotalSeconds ?? 3600);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hybrid cache: {CacheKey}", cacheKey);
        }
    }

    public async Task RemoveAsync(string cacheKey)
    {
        try
        {
            // Remove from both caches
            await Task.WhenAll(
                _l1Cache.RemoveAsync(cacheKey),
                _l2Cache.RemoveAsync(cacheKey)
            );

            _logger.LogDebug("Hybrid cache entry removed: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing from hybrid cache: {CacheKey}", cacheKey);
        }
    }

    public async Task RemoveByTenantAsync(string tenantId)
    {
        try
        {
            // Remove from both caches
            await Task.WhenAll(
                _l1Cache.RemoveByTenantAsync(tenantId),
                _l2Cache.RemoveByTenantAsync(tenantId)
            );

            _logger.LogInformation("Hybrid cache invalidated for tenant: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating hybrid cache for tenant: {TenantId}", tenantId);
        }
    }

    public async Task<bool> ExistsAsync(string cacheKey)
    {
        try
        {
            // Check both caches
            var l1Exists = await _l1Cache.ExistsAsync(cacheKey);
            if (l1Exists) return true;

            var l2Exists = await _l2Cache.ExistsAsync(cacheKey);
            return l2Exists;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking cache existence: {CacheKey}", cacheKey);
            return false;
        }
    }

    public async Task<TimeSpan?> GetTtlAsync(string cacheKey)
    {
        try
        {
            // Prefer L1 TTL, fall back to L2
            var l1Ttl = await _l1Cache.GetTtlAsync(cacheKey);
            if (l1Ttl.HasValue) return l1Ttl.Value;

            return await _l2Cache.GetTtlAsync(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting cache TTL: {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task ClearAllAsync()
    {
        try
        {
            // Clear both caches
            await Task.WhenAll(
                _l1Cache.ClearAllAsync(),
                _l2Cache.ClearAllAsync()
            );

            _logger.LogWarning("All hybrid cache entries cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing hybrid cache");
        }
    }

    public async Task<CacheStrategyStats> GetStatsAsync()
    {
        try
        {
            var l1Stats = await _l1Cache.GetStatsAsync();
            var l2Stats = await _l2Cache.GetStatsAsync();

            var totalHits = _l1Hits + _l2Hits;
            var totalRequests = totalHits + _misses;

            return new CacheStrategyStats
            {
                BackendName = "Hybrid (In-Memory + Redis)",
                TotalEntries = l1Stats.TotalEntries,
                CacheSizeBytes = l1Stats.CacheSizeBytes + l2Stats.CacheSizeBytes,
                HitCount = totalHits,
                MissCount = _misses,
                AdditionalMetrics = new()
                {
                    { "l1_hits", _l1Hits },
                    { "l2_hits", _l2Hits },
                    { "l1_hit_rate", Math.Round((_l1Hits / (double)(totalRequests + 1)) * 100, 2) },
                    { "l2_hit_rate", Math.Round((_l2Hits / (double)(totalRequests + 1)) * 100, 2) },
                    { "l1_entries", l1Stats.TotalEntries },
                    { "l2_entries", l2Stats.TotalEntries },
                    { "l1_size_bytes", l1Stats.CacheSizeBytes },
                    { "l2_size_bytes", l2Stats.CacheSizeBytes }
                },
                CollectedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hybrid cache stats");
            return new CacheStrategyStats { BackendName = "Hybrid (Error)" };
        }
    }

    public string GetBackendName() => "Hybrid (In-Memory + Redis)";
}
