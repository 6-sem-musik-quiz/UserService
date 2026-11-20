namespace UserService.Domain.Entities;

public class User
{
    public int Id { get; set; }

    // Unique, compared case-insensitively (citext column in Postgres).
    public string UserName { get; set; } = string.Empty;

    // ASP.NET PasswordHasher<User> output; the salt is embedded in the hash.
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Player;
}
