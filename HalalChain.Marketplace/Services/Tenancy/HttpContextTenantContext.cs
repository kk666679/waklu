using System.Security.Claims;
using HalalChain.Application.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HalalChain.Marketplace.Services.Tenancy;

/// <summary>
/// Resolves the ambient tenant for the current request.
/// Resolution order: route value → tenant claim → configured default.
/// Tenant isolation is mandatory (P6), so an unresolved tenant falls back to a
/// single configured default rather than leaving <see cref="TenantId"/> empty.
/// </summary>
public sealed class HttpContextTenantContext : ITenantContext
{
    internal const string TenantClaimType = "tenant_id";
    internal const string RouteKey = "tenantId";

    private static readonly IReadOnlySet<string> EmptyCapabilities =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private string? _tenantId;
    private IReadOnlySet<string>? _capabilities;

    public HttpContextTenantContext(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public string TenantId => _tenantId ??= Resolve();

    public IReadOnlySet<string> Capabilities => _capabilities ??= ResolveCapabilities();

    public bool IsResolved => TenantId.Length > 0;

    private string Resolve()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is not null)
        {
            if (context.Request.RouteValues.TryGetValue(RouteKey, out var routeValue)
                && routeValue is string routeTenant
                && !string.IsNullOrWhiteSpace(routeTenant))
            {
                return routeTenant.Trim();
            }

            var claim = context.User?.FindFirst(TenantClaimType)?.Value;
            if (!string.IsNullOrWhiteSpace(claim))
            {
                return claim.Trim();
            }
        }

        var configured = _configuration["Tenancy:DefaultTenantId"];
        return string.IsNullOrWhiteSpace(configured) ? "default" : configured.Trim();
    }

    private IReadOnlySet<string> ResolveCapabilities()
    {
        var context = _httpContextAccessor.HttpContext;
        var values = context?.User
            .FindAll("capability")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();

        return values is { Length: > 0 }
            ? new HashSet<string>(values, StringComparer.OrdinalIgnoreCase)
            : EmptyCapabilities;
    }
}
