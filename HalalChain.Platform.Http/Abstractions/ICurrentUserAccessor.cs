namespace HalalChain.Platform.Http.Abstractions;

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    Guid RequireUserId();
}