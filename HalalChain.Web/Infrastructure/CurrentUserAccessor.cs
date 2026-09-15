using HalalChain.Platform.Http.Abstractions;
using HalalChain.Services;

namespace HalalChain.Web.Infrastructure;

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IAuthService _auth;

    public CurrentUserAccessor(IAuthService auth)
    {
        _auth = auth;
    }

    public Guid? UserId => JwtTokenReader.ReadUserId(_auth.Token);
    public string? Email => _auth.Email;
    public string? DisplayName => _auth.FullName ?? _auth.UserName;
    public bool IsAuthenticated => _auth.IsAuthenticated;
    public IReadOnlyList<string> Roles => _auth.Roles;

    public Guid RequireUserId()
    {
        var id = UserId;
        if (id.HasValue) return id.Value;
        throw new UnauthorizedAccessException("User is not authenticated or has no valid user ID.");
    }
}