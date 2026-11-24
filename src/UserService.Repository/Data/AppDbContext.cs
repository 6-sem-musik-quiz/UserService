using UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UserService.Repository.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // citext gives case-insensitive comparison and uniqueness for User.UserName.
        modelBuilder.HasPostgresExtension("citext");

        // Picks up every IEntityTypeConfiguration<T> in this assembly (see Data/Configurations).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
