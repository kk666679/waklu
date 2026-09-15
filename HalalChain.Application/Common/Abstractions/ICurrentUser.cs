namespace HalalChain.Application.Common.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }
    Guid RequireUserId();
    bool IsInRole(string role);
}

public static class CurrentUserExtensions
{
    public static Guid RequireUserId(this ICurrentUser user) =>
        user.UserId ?? throw new ForbiddenException("No authenticated user.");
}
