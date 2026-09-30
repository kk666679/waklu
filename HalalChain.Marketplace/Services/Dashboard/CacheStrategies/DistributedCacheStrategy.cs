namespace HalalChain.Marketplace.Services.Dashboard.CacheStrategies;

using HalalChain.Marketplace.Models.Dashboard;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Redis-backed distributed cache strategy for dashboard configurations.
/// Provides cross-instance caching with automatic serialization/deserialization.
/// </summary>
public class DistributedCacheStrategy : IDashboardCacheStrategy
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheStrategy> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private int _hits;
    private int _misses;

    public DistributedCacheStrategy(
        IDistributedCache cache,
        ILogger<DistributedCacheStrategy> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = false
        };
    }

    public async Task<TenantDashboardConfig?> GetAsync(string cacheKey)
    {
        try
        {
            var data = await _cache.GetAsync(cacheKey);
            if (data == null)
            {
                _misses++;
                _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
                return null;
            }

            _hits++;
            var json = System.Text.Encoding.UTF8.GetString(data);
            var config = JsonSerializer.Deserialize<TenantDashboardConfig>(json, _jsonOptions);

            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving from distributed cache: {CacheKey}", cacheKey);
            _misses++;
            return null;
        }
    }

    public async Task SetAsync(string cacheKey, TenantDashboardConfig config, TimeSpan? expiration = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
                _logger.LogDebug(
                    "Setting cache with TTL: {CacheKey}, TTL={TTL}s",
                    cacheKey, expiration.Value.TotalSeconds);
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                _logger.LogDebug("Setting cache with default TTL: {CacheKey}", cacheKey);
            }

            await _cache.SetAsync(cacheKey, data, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting distributed cache: {CacheKey}", cacheKey);
        }
    }

    public async Task RemoveAsync(string cacheKey)
    {
        try
        {
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Removed cache entry: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing from distributed cache: {CacheKey}", cacheKey);
        }
    }

    public async Task RemoveByTenantAsync(string tenantId)
    {
        try
        {
            // Note: Redis doesn't support pattern-based deletion efficiently
            // This is a limitation - in practice, you'd use a tag-based approach or track keys separately
            var key = $"dashboard:config:{tenantId}";
            await RemoveAsync(key);
            _logger.LogInformation("Invalidated cache for tenant: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating tenant cache: {TenantId}", tenantId);
        }
    }

    public async Task<bool> ExistsAsync(string cacheKey)
    {
        try
        {
            var data = await _cache.GetAsync(cacheKey);
            return data != null;
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
            // StackExchange.Redis distributed cache doesn't expose TTL directly
            // This is a limitation - you'd need direct Redis client access
            var exists = await ExistsAsync(cacheKey);
            return exists ? TimeSpan.FromHours(1) : null;
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
            // Note: IDistributedCache doesn't support clearing all keys
            // This would require direct Redis client access
            _logger.LogWarning("ClearAllAsync not fully supported for distributed cache - use Redis CLI FLUSHALL");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing distributed cache");
        }
    }

    public async Task<CacheStrategyStats> GetStatsAsync()
    {
        return await Task.FromResult(new CacheStrategyStats
        {
            BackendName = "Redis (Distributed)",
            HitCount = _hits,
            MissCount = _misses,
            AdditionalMetrics = new()
            {
                { "implementation", "StackExchange.Redis" },
                { "note", "Full statistics require direct Redis client access" }
            },
            CollectedAt = DateTime.UtcNow
        });
    }

    public string GetBackendName() => "Redis (Distributed)";
}
