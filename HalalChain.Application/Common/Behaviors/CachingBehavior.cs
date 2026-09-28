namespace HalalChain.Application.Common.Behaviors;

using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Caches query responses via HybridCache (L1 in-process + L2 distributed,
/// with stampede protection).
///
/// Only applies to queries that opt in via ICacheableQuery. Commands never cache.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly HybridCache _cache;

    public CachingBehavior(HybridCache cache) => _cache = cache;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheable)
            return await next();

        var options = new HybridCacheEntryOptions
        {
            Expiration = cacheable.Expiration,
            LocalCacheExpiration = cacheable.LocalExpiration,
        };

        return await _cache.GetOrCreateAsync(
            cacheable.CacheKey,
            async token => await next(),
            options,
            tags: cacheable.Tags,
            cancellationToken: ct);
    }
}
