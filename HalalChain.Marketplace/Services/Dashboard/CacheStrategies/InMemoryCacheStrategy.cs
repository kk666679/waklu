namespace HalalChain.Marketplace.Services.Dashboard.CacheStrategies;

using HalalChain.Marketplace.Models.Dashboard;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;

/// <summary>
/// In-memory cache strategy for dashboard configurations.
/// Provides fast local caching with automatic expiration.
/// Useful as fallback when distributed cache is unavailable.
/// </summary>
public class InMemoryCacheStrategy : IDashboardCacheStrategy
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryCacheStrategy> _logger;
    private readonly ConcurrentDictionary<string, CacheEntryMetadata> _metadata;
    private int _hits;
    private int _misses;

    public InMemoryCacheStrategy(
        IMemoryCache cache,
        ILogger<InMemoryCacheStrategy> logger)
    {
        _cache = cache;
        _logger = logger;
        _metadata = new ConcurrentDictionary<string, CacheEntryMetadata>();
    }

    public async Task<TenantDashboardConfig?> GetAsync(string cacheKey)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (_cache.TryGetValue(cacheKey, out TenantDashboardConfig? config))
                {
                    _hits++;
                    if (_metadata.TryGetValue(cacheKey, out var meta))
                    {
                        meta.LastAccessTime = DateTime.UtcNow;
                        meta.AccessCount++;
                    }

                    _logger.LogDebug("In-memory cache hit: {CacheKey}", cacheKey);
                    return config;
                }

                _misses++;
                _logger.LogDebug("In-memory cache miss: {CacheKey}", cacheKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving from memory cache: {CacheKey}", cacheKey);
                _misses++;
                return null;
            }
        });
    }

    public async Task SetAsync(string cacheKey, TenantDashboardConfig config, TimeSpan? expiration = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                var cacheOptions = new MemoryCacheEntryOptions();

                if (expiration.HasValue)
                {
                    cacheOptions.AbsoluteExpirationRelativeToNow = expiration.Value;
                    _logger.LogDebug(
                        "Setting memory cache with TTL: {CacheKey}, TTL={TTL}s",
                        cacheKey, expiration.Value.TotalSeconds);
                }
                else
                {
                    cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    _logger.LogDebug("Setting memory cache with default TTL: {CacheKey}", cacheKey);
                }

                // Add post-eviction callback for tracking
                cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    _logger.LogDebug("Cache entry evicted: {CacheKey}, Reason: {Reason}", key, reason);
                    _metadata.TryRemove(key.ToString() ?? "", out _);
                });

                _cache.Set(cacheKey, config, cacheOptions);

                // Track metadata
                _metadata[cacheKey] = new CacheEntryMetadata
                {
                    CreatedTime = DateTime.UtcNow,
                    LastAccessTime = DateTime.UtcNow,
                    AccessCount = 0,
                    Expiration = expiration ?? TimeSpan.FromHours(1),
                    SizeBytes = EstimateSize(config)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting memory cache: {CacheKey}", cacheKey);
            }
        });
    }

    public async Task RemoveAsync(string cacheKey)
    {
        return await Task.Run(() =>
        {
            try
            {
                _cache.Remove(cacheKey);
                _metadata.TryRemove(cacheKey, out _);
                _logger.LogDebug("Removed memory cache entry: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error removing from memory cache: {CacheKey}", cacheKey);
            }
        });
    }

    public async Task RemoveByTenantAsync(string tenantId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var prefix = $"dashboard:config:{tenantId}";
                var keysToRemove = _metadata.Keys
                    .Where(k => k.StartsWith(prefix))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                    _metadata.TryRemove(key, out _);
                }

                _logger.LogInformation(
                    "Invalidated memory cache for tenant {TenantId}: removed {Count} entries",
                    tenantId, keysToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating tenant cache: {TenantId}", tenantId);
            }
        });
    }

    public async Task<bool> ExistsAsync(string cacheKey)
    {
        return await Task.Run(() =>
        {
            try
            {
                return _cache.TryGetValue(cacheKey, out _);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking cache existence: {CacheKey}", cacheKey);
                return false;
            }
        });
    }

    public async Task<TimeSpan?> GetTtlAsync(string cacheKey)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (_metadata.TryGetValue(cacheKey, out var meta))
                {
                    var expiresAt = meta.CreatedTime.Add(meta.Expiration);
                    var remaining = expiresAt - DateTime.UtcNow;
                    return remaining > TimeSpan.Zero ? remaining : null;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting cache TTL: {CacheKey}", cacheKey);
                return null;
            }
        });
    }

    public async Task ClearAllAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // IMemoryCache doesn't support clearing all entries
                // We can only track and log what we know about
                var count = _metadata.Count;
                _metadata.Clear();
                _logger.LogWarning(
                    "Memory cache clear requested - cleared {Count} tracked entries (total may be higher)",
                    count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing memory cache");
            }
        });
    }

    public async Task<CacheStrategyStats> GetStatsAsync()
    {
        return await Task.FromResult(new CacheStrategyStats
        {
            BackendName = "In-Memory (Local)",
            TotalEntries = _metadata.Count,
            CacheSizeBytes = _metadata.Values.Sum(m => m.SizeBytes),
            HitCount = _hits,
            MissCount = _misses,
            AdditionalMetrics = new()
            {
                { "implementation", "Microsoft.Extensions.Caching.Memory" },
                { "totalHitRate", Math.Round((_hits / (double)(_hits + _misses + 1)) * 100, 2) },
                { "avgAccessCount", _metadata.Count > 0 ? _metadata.Values.Average(m => m.AccessCount) : 0 },
                { "oldestEntry", _metadata.Values.OrderBy(m => m.CreatedTime).FirstOrDefault()?.CreatedTime ?? DateTime.UtcNow }
            },
            CollectedAt = DateTime.UtcNow
        });
    }

    public string GetBackendName() => "In-Memory (Local)";

    private long EstimateSize(TenantDashboardConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config);
            return json.Length * sizeof(char);
        }
        catch
        {
            return 1024; // Fallback estimate
        }
    }

    /// <summary>Metadata about a cache entry.</summary>
    private class CacheEntryMetadata
    {
        public DateTime CreatedTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public int AccessCount { get; set; }
        public TimeSpan Expiration { get; set; }
        public long SizeBytes { get; set; }
    }
}
