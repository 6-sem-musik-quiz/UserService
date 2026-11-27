using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Repository.Data;
using UserService.Repository.Interfaces;

namespace UserService.Repository.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    // The citext column already compares case-insensitively in Postgres; ToLower() keeps the
    // behaviour identical on providers without citext (e.g. EF InMemory in tests).
    // User names are restricted to ASCII, so lower-casing is unambiguous.
    public Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct = default)
    {
        var normalized = userName.ToLower();
        return context.Users.AnyAsync(u => u.UserName.ToLower() == normalized, ct);
    }

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default)
    {
        var normalized = userName.ToLower();
        return context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName.ToLower() == normalized, ct);
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        context.Users.Add(user);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another request created the same name between our check and the insert.
            context.Entry(user).State = EntityState.Detached;
            throw new DuplicateUserNameException(user.UserName, ex);
        }
        return user;
    }

    public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await context.Users.AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .ToListAsync(ct);
    }

    // Load-then-remove instead of ExecuteDeleteAsync: EF InMemory (used by the API tests) does not
    // support ExecuteDelete. A load test creates at most a few hundred bots, so loading them is cheap.
    public async Task<int> DeleteByUserNamePrefixAsync(string prefix, CancellationToken ct = default)
    {
        var normalized = prefix.ToLower();
        var users = await context.Users
            .Where(u => u.UserName.ToLower().StartsWith(normalized))
            .ToListAsync(ct);
        context.Users.RemoveRange(users);
        await context.SaveChangesAsync(ct);
        return users.Count;
    }
}
