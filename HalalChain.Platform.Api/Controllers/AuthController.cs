using System.Security.Cryptography;
using System.Text;
using HalalChain.Application.Common.Abstractions;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Api.Modules.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalChain.Platform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ITokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and password are required." });

        var userId = DeterministicGuid(request.Email);
        var displayName = request.Email.Split('@')[0];
        var (token, _) = tokenService.Issue(userId, request.Email, displayName, new[] { AuthConstants.RoleMarketplaceUser });
        return Ok(new LoginResponse(token, displayName, displayName, new[] { AuthConstants.RoleMarketplaceUser }));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public ActionResult<LoginResponse> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Email, password, and name are required." });

        var userId = DeterministicGuid(request.Email);
        var (token, _) = tokenService.Issue(userId, request.Email, request.Name, new[] { AuthConstants.RoleMarketplaceUser });
        return Ok(new LoginResponse(token, request.Name, request.Name, new[] { AuthConstants.RoleMarketplaceUser }));
    }

    /// <summary>
    /// Generates a stable Guid from the email address so that the same user
    /// always receives the same <c>sub</c> claim across logins, even without
    /// a persisted user table. Different emails always produce different Guids.
    /// </summary>
    private static Guid DeterministicGuid(string email)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes($"halalchain-user:{email.Trim().ToLowerInvariant()}"));
        return new Guid(hash.Take(16).ToArray());
    }
}
