namespace HalalChain.Platform.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string Email, string Password, string Name);
public sealed record LoginResponse(string? Token, string? UserName, string? FullName, string[]? Roles);
