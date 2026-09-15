using HalalChain.Platform.Http.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace HalalChain.Marketplace.Services;

public sealed class HttpContextUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var guid) ? guid : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;
    public string? DisplayName => User?.FindFirst(ClaimTypes.Name)?.Value;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];

    public Guid RequireUserId() => UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");
}