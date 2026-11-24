using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UserService.Repository.Data;

namespace UserService.Api.Tests;

// Runs the real API pipeline but swaps Postgres for an isolated EF InMemory database.
public class UserServiceApiFactory : WebApplicationFactory<Program>
{
    // Exposed so tests can sign/validate tokens consistently.
    public const string TestJwtSigningKey = "integration-test-signing-key-at-least-32-bytes-long";

    private readonly string _databaseName = $"users-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Inject a valid signing key so the startup validation and TokenService work in tests.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = TestJwtSigningKey
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the Npgsql registration (options + the EF 9+ options configuration callback).
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
