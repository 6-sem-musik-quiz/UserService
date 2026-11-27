using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace UserService.Api.Tests;

// SCRUM-197: DELETE /api/admin/users?prefix=loadbot_ removes load-test bots. Admin only.
public class AdminUsersEndpointTests(UserServiceApiFactory factory) : IClassFixture<UserServiceApiFactory>
{
    private const string Password = "correct-horse";

    [Fact]
    public async Task Delete_AsAdmin_RemovesOnlyLoadBots()
    {
        var client = factory.CreateClient();
        var bot1 = await CreateUserAsync(client, "loadbot_1");
        await CreateUserAsync(client, "loadbot_2");
        var alice = await CreateUserAsync(client, "alice_admin_test");

        var response = await Delete(client, "loadbot_", Token("Admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("deleted").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/users/{alice}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/users/{bot1}")).StatusCode);
    }

    // A real player may pick "LoadBot_Mads" (the name rule allows it). Only the exact lower-case
    // prefix the k6 bots use is deleted, the same way StatisticService matches it.
    [Fact]
    public async Task Delete_AsAdmin_KeepsNamesThatOnlyDifferInCase()
    {
        var client = factory.CreateClient();
        var player = await CreateUserAsync(client, "LoadBot_Mads");

        var response = await Delete(client, "loadbot_", Token("Admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/users/{player}")).StatusCode);
    }

    [Fact]
    public async Task Delete_AsPlayer_Returns403()
    {
        var response = await Delete(factory.CreateClient(), "loadbot_", Token("Player"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        var response = await Delete(factory.CreateClient(), "loadbot_", token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("loadbot")]
    [InlineData("")]
    public async Task Delete_WithOtherPrefix_Returns400(string prefix)
    {
        var client = factory.CreateClient();
        var userId = await CreateUserAsync(client, $"keep_{Guid.NewGuid():N}"[..20]);

        var response = await Delete(client, prefix, Token("Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/users/{userId}")).StatusCode);
    }

    private static Task<HttpResponseMessage> Delete(HttpClient client, string prefix, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users?prefix={prefix}");
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }

    private static async Task<int> CreateUserAsync(HttpClient client, string userName)
    {
        var response = await client.PostAsJsonAsync("/api/users", new { userName, password = Password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetInt32();
    }

    // Signed like UserService's own TokenService: HS256, issuer/audience, claims sub + role.
    private static string Token(string role)
    {
        var jwt = new JwtSecurityToken(
            issuer: "slopster-userservice", audience: "slopster",
            claims: [new Claim("sub", "999"), new Claim("role", role)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(UserServiceApiFactory.TestJwtSigningKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
