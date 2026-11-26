using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UserService.Domain.Entities;
using UserService.Service.Services;

namespace UserService.Service.Tests;

public class TokenServiceTests
{
    private static TokenService BuildSut(string? signingKey = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = signingKey ?? "valid-signing-key-that-is-at-least-32-bytes"
            })
            .Build();
        return new TokenService(config);
    }

    private static User TestUser => new()
    {
        Id = 7,
        UserName = "alice",
        Role = UserRole.Player
    };

    [Fact]
    public void CreateToken_ValidUser_ReturnsSignedJwtWithCorrectClaims()
    {
        var sut = BuildSut();

        var (tokenString, expiresAt) = sut.CreateToken(TestUser);

        Assert.False(string.IsNullOrEmpty(tokenString));

        // Read the raw JWT claims without ASP.NET claim type mapping.
        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("valid-signing-key-that-is-at-least-32-bytes")),
            ValidIssuer = "slopster-userservice",
            ValidAudience = "slopster",
            ClockSkew = TimeSpan.Zero
        };

        var principal = handler.ValidateToken(tokenString, validationParams, out _);

        Assert.Equal("7", principal.FindFirst("sub")?.Value);
        Assert.Equal("alice", principal.FindFirst("unique_name")?.Value);
        Assert.Equal("Player", principal.FindFirst("role")?.Value);
        Assert.Equal("slopster-userservice", principal.FindFirst("iss")?.Value);
        Assert.True(expiresAt > DateTime.UtcNow.AddMinutes(14));
        Assert.True(expiresAt <= DateTime.UtcNow.AddMinutes(15));
    }

    [Fact]
    public void CreateToken_AdminUser_SetsAdminRoleClaim()
    {
        var sut = BuildSut();
        var admin = new User { Id = 1, UserName = "admin", Role = UserRole.Admin };

        var (tokenString, _) = sut.CreateToken(admin);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        Assert.Equal("Admin", token.Claims.First(c => c.Type == "role").Value);
    }

    [Fact]
    public void CreateToken_MissingKey_Throws()
    {
        var config = new ConfigurationBuilder().Build();
        var sut = new TokenService(config);

        Assert.Throws<InvalidOperationException>(() => sut.CreateToken(TestUser));
    }

    [Fact]
    public void CreateToken_KeyTooShort_Throws()
    {
        var sut = BuildSut("too-short");

        Assert.Throws<InvalidOperationException>(() => sut.CreateToken(TestUser));
    }
}
