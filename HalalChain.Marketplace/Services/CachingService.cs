using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HalalChain.Marketplace.Services;

/// <summary>
/// Distributed caching service for marketplace data.
/// Caches frequently accessed data (products, vendors, categories) to reduce database queries.
/// </summary>
public interface ICachingService
{
    /// <summary>Get cached data or fetch and cache if not present.</summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>Get cached data directly.</summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>Set cached data.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>Remove cached data.</summary>
    Task RemoveAsync(string key);

    /// <summary>Remove multiple cached keys with pattern matching.</summary>
    Task RemoveByPatternAsync(string pattern);
}

/// <summary>Implementation using Redis or in-memory cache.</summary>
public class DistributedCachingService : ICachingService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCachingService> _logger;

    // Cache key patterns for invalidation
    public const string PRODUCTS_KEY = "products";
    public const string VENDORS_KEY = "vendors";
    public const string CATEGORIES_KEY = "categories";
    public const string VERIFICATIONS_KEY = "verifications";
    public const string CERTIFICATES_KEY = "certificates";

    // Default cache durations
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ShortDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LongDuration = TimeSpan.FromHours(1);

    public DistributedCachingService(IDistributedCache cache, ILogger<DistributedCachingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        try
        {
            // Try to get from cache
            var cached = await GetAsync<T>(key);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cached;
            }

            // Cache miss - fetch and store
            _logger.LogDebug("Cache miss for key: {Key}", key);
            var value = await factory();
            
            if (value != null)
            {
                await SetAsync(key, value, expiration);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            // Return factory result on cache error
            return await factory();
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var data = await _cache.GetAsync(key);
            if (data == null)
                return default;

            var json = System.Text.Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultDuration
            };

            await _cache.SetAsync(key, data, options);
            _logger.LogDebug("Cached data for key: {Key}, duration: {Duration}", key, expiration ?? DefaultDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("Removed cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            // Note: Distributed cache doesn't support pattern removal directly
            // This would need to be implemented with Redis-specific commands
            // For now, we log a note about needing Redis-specific implementation
            _logger.LogWarning("Pattern-based cache invalidation requires Redis-specific implementation");
            // TODO: Implement with Redis IConnectionMultiplexer for KEYS pattern matching
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
        }
    }
}

/// <summary>Cache key factory for consistent key naming.</summary>
public static class CacheKeys
{
    public static string ProductById(Guid id) => $"product:{id}";
    public static string ProductsByVendor(Guid vendorId) => $"products:vendor:{vendorId}";
    public static string ProductsByCategory(Guid categoryId) => $"products:category:{categoryId}";
    public static string ProductsBySearch(string query) => $"products:search:{query.GetHashCode()}";
    
    public static string VendorById(Guid id) => $"vendor:{id}";
    public static string VendorsByRating(int minRating) => $"vendors:rating:{minRating}";
    
    public static string VerificationById(Guid id) => $"verification:{id}";
    public static string VerificationsByStatus(string status) => $"verifications:status:{status}";
    
    public static string CertificateById(Guid id) => $"certificate:{id}";
    public static string CertificatesByProduct(Guid productId) => $"certificates:product:{productId}";
    
    public static string CategoriesList => "categories:list";
    public static string DepartmentsList => "departments:list";
    public static string CertificationBodiesList => "certification-bodies:list";
    
    public static string TopProductsForVendor(Guid vendorId, string sortBy, int take) 
        => $"top-products:vendor:{vendorId}:{sortBy}:{take}";
    
    public static string OrdersByCustomer(Guid customerId) => $"orders:customer:{customerId}";
    public static string OrdersByStatus(string status) => $"orders:status:{status}";
}
