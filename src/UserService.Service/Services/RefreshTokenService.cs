using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;
using UserService.Repository.Interfaces;
using UserService.Service.Exceptions;
using UserService.Service.Interfaces;

namespace UserService.Service.Services;

public class RefreshTokenService(
    IRefreshTokenRepository repository,
    IConfiguration configuration,
    ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    private const int DefaultLifetimeDays = 7;

    public async Task<(string PlainToken, DateTime ExpiresAt)> CreateAsync(int userId, CancellationToken ct = default)
    {
        var (plainToken, hash) = GenerateToken();
        var expiresAt = DateTime.UtcNow.AddDays(GetLifetimeDays());

        var token = new RefreshToken
        {
            TokenHash = hash,
            FamilyId = Guid.NewGuid(),
            UserId = userId,
            ExpiresAt = expiresAt,
        };

        await repository.AddAsync(token, ct);
        return (plainToken, expiresAt);
    }

    public async Task<(string PlainToken, DateTime ExpiresAt, User User)> RotateAsync(string plainToken, CancellationToken ct = default)
    {
        var hash = HashToken(plainToken);
        var existing = await repository.GetByTokenHashAsync(hash, ct);

        if (existing is null || existing.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidRefreshTokenException();

        if (existing.IsRevoked)
        {
            logger.LogWarning(
                "Refresh token reuse detected for FamilyId {FamilyId}, UserId {UserId}. Revoking entire family.",
                existing.FamilyId, existing.UserId);
            await repository.RevokeByFamilyIdAsync(existing.FamilyId, ct);
            throw new InvalidRefreshTokenException();
        }

        existing.IsRevoked = true;
        await repository.UpdateAsync(existing, ct);

        var (newPlainToken, newHash) = GenerateToken();
        var expiresAt = DateTime.UtcNow.AddDays(GetLifetimeDays());

        var newToken = new RefreshToken
        {
            TokenHash = newHash,
            FamilyId = existing.FamilyId,
            UserId = existing.UserId,
            ExpiresAt = expiresAt,
        };

        await repository.AddAsync(newToken, ct);
        return (newPlainToken, expiresAt, existing.User);
    }

    public async Task RevokeAsync(string? plainToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(plainToken))
            return;

        var hash = HashToken(plainToken);
        var existing = await repository.GetByTokenHashAsync(hash, ct);

        if (existing is null || existing.IsRevoked)
            return;

        existing.IsRevoked = true;
        await repository.UpdateAsync(existing, ct);
    }

    private static (string PlainToken, string Hash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var plainToken = Base64UrlEncode(bytes);
        return (plainToken, HashToken(plainToken));
    }

    private static string HashToken(string plainToken)
    {
        var inputBytes = Encoding.UTF8.GetBytes(plainToken);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private int GetLifetimeDays() =>
        configuration.GetValue("Jwt:RefreshTokenLifetimeDays", DefaultLifetimeDays);
}
