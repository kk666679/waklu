using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Models;
using Microsoft.AspNetCore.Components;

namespace HalalChain.Marketplace.Services;

/// <summary>Authentication state for the marketplace Blazor app. Owns the JWT
/// issued by login/register and exposes it via <see cref="Token"/>. Also
/// implements <see cref="IPlatformTokenAccessor"/> so the shared typed HttpClient
/// can attach the bearer header without a separate wrapper — this avoids the
/// circular dependency that a wrapper accessor would create
/// (wrapper -> AuthService -> wrapper).</summary>
public sealed class AuthService : IPlatformTokenAccessor
{
    private readonly IApiClient _api;
    private readonly NavigationManager _nav;

    public AuthService(IApiClient api, NavigationManager nav)
    {
        _api = api;
        _nav = nav;
    }

    public string? Token { get; private set; }
    public string? UserName { get; private set; }
    public string[] Roles { get; private set; } = Array.Empty<string>();
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public event Action? OnAuthStateChanged;

    Task<string?> IPlatformTokenAccessor.GetAccessTokenAsync(CancellationToken ct)
        => Task.FromResult(Token);

    Task<bool> IPlatformTokenAccessor.IsAuthenticatedAsync(CancellationToken ct)
        => Task.FromResult(IsAuthenticated);

    public async Task<Result<LoginResponse>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var result = await _api.PostAsync<LoginResponse>("/api/auth/login", new { email, password }, ct: ct);
        if (result.IsSuccess && result.Value?.Token != null)
        {
            Token = result.Value.Token;
            UserName = result.Value.UserName ?? email.Split('@')[0];
            Roles = result.Value.Roles ?? new[] { "marketplace-user" };
            OnAuthStateChanged?.Invoke();
        }
        return result;
    }

    public async Task<Result<LoginResponse>> RegisterAsync(string email, string password, string name, CancellationToken ct = default)
    {
        var result = await _api.PostAsync<LoginResponse>("/api/auth/register", new { email, password, name }, ct: ct);
        if (result.IsSuccess && result.Value?.Token != null)
        {
            Token = result.Value.Token;
            UserName = result.Value.UserName ?? name;
            Roles = result.Value.Roles ?? new[] { "marketplace-user" };
            OnAuthStateChanged?.Invoke();
        }
        return result;
    }

    public async Task LogoutAsync()
    {
        Token = null;
        UserName = null;
        Roles = Array.Empty<string>();
        OnAuthStateChanged?.Invoke();
        _nav.NavigateTo("/account/logout", forceLoad: true);
    }
}