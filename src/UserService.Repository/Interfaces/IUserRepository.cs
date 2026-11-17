using UserService.Domain.Entities;

namespace UserService.Repository.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
}
