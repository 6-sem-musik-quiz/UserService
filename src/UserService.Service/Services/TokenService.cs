using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UserService.Domain.Entities;
using UserService.Service.Interfaces;

namespace UserService.Service.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    private const string Issuer = "slopster-userservice";
    private const string Audience = "slopster";
    private const int MinKeyBytes = 32;
    private const int DefaultLifetimeHours = 12;

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var keyString = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        if (Encoding.UTF8.GetByteCount(keyString) < MinKeyBytes)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");

        var lifetimeHours = configuration.GetValue("Jwt:LifetimeHours", DefaultLifetimeHours);
        var expiresAt = DateTime.UtcNow.AddHours(lifetimeHours);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim("role", user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
