using HalalChain.Platform.Http.Abstractions;
using HalalChain.Services;

namespace HalalChain.Web.Infrastructure;

public sealed class PlatformTokenAccessor : IPlatformTokenAccessor
{
    private readonly IAuthService _auth;

    public PlatformTokenAccessor(IAuthService auth)
    {
        _auth = auth;
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken ct = default) =>
        Task.FromResult(_auth.Token);

    public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) =>
        Task.FromResult(_auth.IsAuthenticated);
}