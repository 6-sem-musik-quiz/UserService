using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserService.Repository.Data;

// Used by EF Core tools (dotnet ef migrations add) so they don't need to run the full
// startup project, which requires a live Jwt:SigningKey and database connection.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=migrations-only;Username=postgres")
            .Options;
        return new AppDbContext(options);
    }
}
