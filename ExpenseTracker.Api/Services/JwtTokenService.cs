using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Dtos.Auth;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.Api.Services;

// JwtTokenService creates the signed bearer token returned after login or registration.
public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public AuthResponse CreateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);
        // These claims are what the rest of the app reads from HttpContext.User later.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAt,
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email
            }
        };
    }
}
