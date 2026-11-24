using System.Net;

namespace UserService.Api.Tests;

// Uses UserServiceApiFactory so the JWT signing key is configured and the real Postgres is not needed.
public class HealthCheckTests(UserServiceApiFactory factory) : IClassFixture<UserServiceApiFactory>
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
