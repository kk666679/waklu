using Microsoft.JSInterop;

namespace HalalChain.Services;

public class AuthService : IAuthService
{
    private const string TokenKey = "halalchain.auth.token";
    private readonly IJSRuntime _js;

    public event Action? OnAuthStateChanged;
    public bool IsAuthenticated { get; private set; }
    public string? Token { get; private set; }
    public string? UserName { get; private set; }
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public IReadOnlyList<string> Roles { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; private set; } = Array.Empty<string>();

    public AuthService(IJSRuntime js) { _js = js; }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return new AuthResult(false, Error: "Email and password are required.");
        Token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{email}:{DateTime.UtcNow.Ticks}"));
        UserName = email; FullName = email; Email = email;
        Roles = new[] { "Customer" };
        Permissions = Array.Empty<string>();
        IsAuthenticated = true;
        await _js.InvokeVoidAsync("localStorage.setItem", ct, TokenKey, Token);
        OnAuthStateChanged?.Invoke();
        return new AuthResult(true, Token: Token, UserId: email);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string fullName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || password.Length < 6)
            return new AuthResult(false, Error: "Email and a 6+ char password are required.");
        Token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{email}:{DateTime.UtcNow.Ticks}"));
        UserName = email; FullName = fullName; Email = email;
        Roles = new[] { "Customer" };
        Permissions = Array.Empty<string>();
        IsAuthenticated = true;
        await _js.InvokeVoidAsync("localStorage.setItem", ct, TokenKey, Token);
        OnAuthStateChanged?.Invoke();
        return new AuthResult(true, Token: Token, UserId: email);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        Token = null; UserName = null; FullName = null; Email = null;
        Roles = Array.Empty<string>();
        Permissions = Array.Empty<string>();
        IsAuthenticated = false;
        await _js.InvokeVoidAsync("localStorage.removeItem", ct, TokenKey);
        OnAuthStateChanged?.Invoke();
    }

    public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
        => Task.FromResult(IsAuthenticated);

    public Task<string?> GetTokenAsync(CancellationToken ct = default)
        => Task.FromResult(Token);
}
