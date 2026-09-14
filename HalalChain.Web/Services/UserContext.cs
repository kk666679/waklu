namespace HalalChain.Services;

public interface IUserContext
{
    bool IsAuthenticated { get; }
    string UserName { get; }
    string FullName { get; }
    string Email { get; }
    string AvatarUrl { get; }
    string[] Roles { get; }
    string[] Permissions { get; }
    event Action? OnChange;
}

public class UserContext : IUserContext
{
    private readonly IAuthService _auth;

    public UserContext(IAuthService auth)
    {
        _auth = auth;
        _auth.OnAuthStateChanged += HandleAuthStateChanged;
    }

    public event Action? OnChange;

    public bool IsAuthenticated => _auth.IsAuthenticated;
    public string UserName => _auth.UserName ?? "";
    public string FullName => _auth.FullName ?? "";
    public string Email => _auth.Email ?? "";
    public string[] Roles => _auth.Roles.ToArray();
    public string[] Permissions => _auth.Permissions.ToArray();

    public string AvatarUrl
    {
        get
        {
            if (!string.IsNullOrEmpty(UserName))
            {
                var initial = char.ToUpper(UserName[0]);
                return $"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 40 40'%3E%3Ccircle cx='20' cy='20' r='20' fill='%2310b981'/%3E%3Ctext x='20' y='26' text-anchor='middle' fill='white' font-size='18' font-family='sans-serif'%3E{initial}%3C/text%3E%3C/svg%3E";
            }
            return "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 40 40'%3E%3Ccircle cx='20' cy='20' r='20' fill='%2310b981'/%3E%3Ctext x='20' y='26' text-anchor='middle' fill='white' font-size='18' font-family='sans-serif'%3EA%3C/text%3E%3C/svg%3E";
        }
    }

    private void HandleAuthStateChanged() => OnChange?.Invoke();
}

public class AnonymousUserContext : IUserContext
{
    public bool IsAuthenticated => false;
    public string UserName => "Guest";
    public string FullName => "Guest User";
    public string Email => "";
    public string AvatarUrl => "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 40 40'%3E%3Ccircle cx='20' cy='20' r='20' fill='%239ca3af'/%3E%3Ctext x='20' y='26' text-anchor='middle' fill='white' font-size='18' font-family='sans-serif'%3EG%3C/text%3E%3C/svg%3E";
    public string[] Roles => [];
    public string[] Permissions => [];
    public event Action? OnChange { add { } remove { } }
}
