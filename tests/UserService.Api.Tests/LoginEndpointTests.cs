using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UserService.Api.Tests;

public class LoginEndpointTests(UserServiceApiFactory factory) : IClassFixture<UserServiceApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> CreateUserAndReturnNameAsync(string userName = "loginuser", string password = "correct-horse")
    {
        var resp = await _client.PostAsJsonAsync("/api/users", new { userName, password });
        resp.EnsureSuccessStatusCode();
        return userName;
    }

    [Fact]
    public async Task Login_CorrectCredentials_Returns200WithAccessTokenAndUserDto()
    {
        const string password = "correct-horse";
        await CreateUserAndReturnNameAsync("LoginOk", password);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { userName = "LoginOk", password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;

        var token = root.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        var expiresAt = root.GetProperty("accessTokenExpiresAt").GetDateTime();
        Assert.True(expiresAt > DateTime.UtcNow.AddMinutes(14));

        var user = root.GetProperty("user");
        Assert.Equal("LoginOk", user.GetProperty("userName").GetString());
        Assert.Equal("Player", user.GetProperty("role").GetString());
        Assert.True(user.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task Login_UserNameIsCaseInsensitive_Returns200()
    {
        const string password = "correct-horse";
        await CreateUserAndReturnNameAsync("CaseUser", password);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { userName = "caseuser", password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401ProblemDetails()
    {
        await CreateUserAndReturnNameAsync("WrongPw", "correct-horse");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { userName = "WrongPw", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(401, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { userName = "nobody", password = "some-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(401, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Login_WrongPassword_SameDetailAsUnknownUser()
    {
        await CreateUserAndReturnNameAsync("SameErr", "correct-horse");

        var r1 = await _client.PostAsJsonAsync("/api/auth/login", new { userName = "SameErr", password = "wrong" });
        var r2 = await _client.PostAsJsonAsync("/api/auth/login", new { userName = "nobody-exists", password = "some-pw" });

        using var b1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync());
        using var b2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync());

        Assert.Equal(
            b1.RootElement.GetProperty("detail").GetString(),
            b2.RootElement.GetProperty("detail").GetString());
    }
}
