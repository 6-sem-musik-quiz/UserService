using UserService.Repository.Data;
using UserService.Repository.Interfaces;
using UserService.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace UserService.Repository.Extensions;

public static class RepositoryLayerExtensions
{
    public static IServiceCollection AddRepositoryLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    // Applies the migrations that are not in the database yet. Each pod runs this on
    // startup, so every image Flux deploys from main brings its schema changes with it.
    // It is safe to run on every start and from several pods at once:
    // - EF records applied migrations in "__EFMigrationsHistory" and skips those.
    // - EF Core 9+ locks that table while migrating, so a second pod waits and then
    //   finds nothing left to apply.
    public static void MigrateDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // The EF InMemory provider used in tests has no migrations.
        if (db.Database.IsRelational())
            db.Database.Migrate();
    }
}
