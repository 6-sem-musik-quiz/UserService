using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Repository.Data;
using UserService.Repository.Interfaces;

namespace UserService.Repository.Repositories;

public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync(ct);
        return token;
    }

    // Tracking query so callers can update the entity without re-attaching.
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task RevokeByFamilyIdAsync(Guid familyId, CancellationToken ct = default)
    {
        var tokens = await context.RefreshTokens
            .Where(t => t.FamilyId == familyId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.IsRevoked = true;

        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        context.RefreshTokens.Update(token);
        await context.SaveChangesAsync(ct);
    }
}
