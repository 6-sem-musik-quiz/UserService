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

    // Returns all users whose Id is in ids; unknown ids are silently skipped.
    Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    // SCRUM-197: deletes every user whose name starts with prefix (case-insensitive) and their
    // refresh tokens (cascade). Returns the number of users deleted. The caller validates the prefix.
    Task<int> DeleteByUserNamePrefixAsync(string prefix, CancellationToken ct = default);
}
