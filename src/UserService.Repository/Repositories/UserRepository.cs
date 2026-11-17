using UserService.Domain.Entities;
using UserService.Repository.Data;
using UserService.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace UserService.Repository.Repositories;

public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default) =>
        Set.AnyAsync(u => u.Username == username, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        Set.AnyAsync(u => u.Email == email, ct);
}
