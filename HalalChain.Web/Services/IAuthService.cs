namespace HalalChain.Services;

public interface IAuthService
{
    event Action? OnAuthStateChanged;
    bool IsAuthenticated { get; }
    string? UserName { get; }
    string? FullName { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    string? Token { get; }

    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<AuthResult> RegisterAsync(string email, string password, string fullName, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
    Task<string?> GetTokenAsync(CancellationToken ct = default);
}

public record AuthResult(bool Success, string? Token = null, string? Error = null, string? UserId = null);
