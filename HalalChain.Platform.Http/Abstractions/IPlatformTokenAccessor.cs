namespace HalalChain.Platform.Http.Abstractions;

/// <summary>
/// Provides the bearer token that should accompany outbound calls to the platform API.
/// One implementation per host: Blazor reads from <c>IUserContext</c>; MVC reads the
/// <c>hc_token</c> cookie or the <c>jwt</c> claim.
/// </summary>
public interface IPlatformTokenAccessor
{
    Task<string?> GetAccessTokenAsync(CancellationToken ct = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
}
