using UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UserService.Repository.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Example> Examples => Set<Example>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration<T> in this assembly (see Data/Configurations).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
