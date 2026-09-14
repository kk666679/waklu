using HalalChain.Platform.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HalalChain.Platform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and password are required." });

        var jwt = config.GetSection("Jwt").Get<JwtSettings>();
        if (jwt is null || string.IsNullOrWhiteSpace(jwt.Key))
            return StatusCode(500, new { error = "JWT is not configured." });

        var token = GenerateToken(jwt, request.Email);
        return Ok(new LoginResponse(token, request.Email.Split('@')[0], request.Email.Split('@')[0], ["marketplace-user"]));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public ActionResult<LoginResponse> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Email, password, and name are required." });

        var jwt = config.GetSection("Jwt").Get<JwtSettings>();
        if (jwt is null || string.IsNullOrWhiteSpace(jwt.Key))
            return StatusCode(500, new { error = "JWT is not configured." });

        var token = GenerateToken(jwt, request.Email);
        return Ok(new LoginResponse(token, request.Name, request.Name, ["marketplace-user"]));
    }

    private string GenerateToken(JwtSettings jwt, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, email.Split('@')[0]),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("role", "marketplace-user")
        };
        var token = new JwtSecurityToken(
            jwt.Issuer,
            jwt.Audience,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(jwt.ExpiresMinutes),
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
