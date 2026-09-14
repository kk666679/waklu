using HalalChain.Platform.Http.Abstractions;
using Microsoft.AspNetCore.Http;

namespace HalalChain.Platform.Http.TokenAccessors;

/// <summary>
/// Reads the bearer token from the <c>hc_token</c> cookie first, then from a
/// <c>jwt</c> claim on the current user. Suitable for the MVC host.
/// </summary>
public sealed class CookieTokenAccessor : IPlatformTokenAccessor
{
    public const string CookieName = "hc_token";
    public const string JwtClaim = "jwt";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieTokenAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return Task.FromResult<string?>(null);

        if (ctx.Request.Cookies.TryGetValue(CookieName, out var cookieToken)
            && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return Task.FromResult<string?>(cookieToken);
        }

        var claimToken = ctx.User?.FindFirst(JwtClaim)?.Value;
        return Task.FromResult(claimToken);
    }

    public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
    {
        var ctx = _httpContextAccessor.HttpContext;
        return Task.FromResult(ctx?.User?.Identity?.IsAuthenticated ?? false);
    }
}
