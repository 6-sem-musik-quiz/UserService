using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UserService.Api.Tests;

public class HealthCheckTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        // There is no Postgres in CI, so don't run migrations on startup.
        var client = factory
            .WithWebHostBuilder(b => b.UseSetting("Database:MigrateOnStartup", "false"))
            .CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
