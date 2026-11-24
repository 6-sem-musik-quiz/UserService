using UserService.Domain.Entities;

namespace UserService.Service.Interfaces;

public interface IRefreshTokenService
{
    Task<(string PlainToken, DateTime ExpiresAt)> CreateAsync(int userId, CancellationToken ct = default);

    // Rotates the token: marks old token as revoked, issues new token with the same FamilyId.
    // Throws InvalidRefreshTokenException if the token is invalid, expired, or already revoked
    // (reuse detection: revoking the entire family before throwing).
    Task<(string PlainToken, DateTime ExpiresAt, User User)> RotateAsync(string plainToken, CancellationToken ct = default);

    // Revokes the token. Idempotent: does not throw if the token is already revoked or unknown.
    Task RevokeAsync(string? plainToken, CancellationToken ct = default);
}
