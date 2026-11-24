using UserService.Domain.Entities;

namespace UserService.Repository.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);

    // Case-insensitive: "Alice" matches an existing "alice".
    Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct = default);

    // Case-insensitive lookup; returns null if not found.
    Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default);

    // Throws DuplicateUserNameException if the unique index on UserName is violated.
    Task<User> AddAsync(User user, CancellationToken ct = default);
}
