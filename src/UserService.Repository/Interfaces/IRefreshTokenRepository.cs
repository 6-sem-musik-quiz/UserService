using UserService.Domain.Entities;

namespace UserService.Repository.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task RevokeByFamilyIdAsync(Guid familyId, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
}
