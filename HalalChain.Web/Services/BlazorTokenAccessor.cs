using HalalChain.Platform.Http.Abstractions;
using HalalChain.Services;

namespace HalalChain.Services;

internal sealed class BlazorTokenAccessor : IPlatformTokenAccessor
{
    private readonly IAuthService _auth;

    public BlazorTokenAccessor(IAuthService auth)
    {
        _auth = auth;
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
        => Task.FromResult(_auth.Token);

    public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
        => Task.FromResult(_auth.IsAuthenticated);
}
