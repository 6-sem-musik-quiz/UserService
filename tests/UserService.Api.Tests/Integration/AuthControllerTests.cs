using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UserService.Api.Tests.Integration;

public class AuthControllerTests(UserServiceApiFactory factory) : IClassFixture<UserServiceApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> CreateUserAsync(string userName = "authuser", string password = "correct-horse")
    {
        var resp = await _client.PostAsJsonAsync("/api/users", new { userName, password });
        resp.EnsureSuccessStatusCode();
        return userName;
    }

    // ── POST /api/auth/login ─────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsBothTokens()
    {
        await CreateUserAsync("LoginBothUser");

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { userName = "LoginBothUser", password = "correct-horse" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;

        var accessToken = root.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(accessToken));

        var refreshToken = root.GetProperty("refreshToken").GetString();
        Assert.False(string.IsNullOrEmpty(refreshToken));

        var refreshExpiresAt = root.GetProperty("refreshTokenExpiresAt").GetDateTime();
        Assert.True(refreshExpiresAt > DateTime.UtcNow.AddDays(6));

        var user = root.GetProperty("user");
        Assert.Equal("LoginBothUser", user.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        await CreateUserAsync("LoginBadUser");

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { userName = "LoginBadUser", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST /api/auth/refresh ───────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        await CreateUserAsync("RefreshOkUser");
        var loginResp = await LoginAsync("RefreshOkUser");
        var refreshToken = loginResp.GetProperty("refreshToken").GetString()!;

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;

        Assert.False(string.IsNullOrEmpty(root.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrEmpty(root.GetProperty("refreshToken").GetString()));
        Assert.NotEqual(refreshToken, root.GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = "this-token-does-not-exist" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_UsedTokenAgain_Returns401()
    {
        await CreateUserAsync("ReuseUser");
        var loginResp = await LoginAsync("ReuseUser");
        var originalToken = loginResp.GetProperty("refreshToken").GetString()!;

        // First use: OK.
        var firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = originalToken });
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        // Reuse original token: should be 401.
        var reuseResp = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = originalToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResp.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReuseDetection_RevokesNewTokenToo()
    {
        await CreateUserAsync("ReuseDetectUser");
        var loginResp = await LoginAsync("ReuseDetectUser");
        var originalToken = loginResp.GetProperty("refreshToken").GetString()!;

        // Rotate once to get T2.
        using var firstBody = JsonDocument.Parse(
            await (await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = originalToken }))
                .Content.ReadAsStringAsync());
        var t2 = firstBody.RootElement.GetProperty("refreshToken").GetString()!;

        // Reuse T1 (revoked) → triggers family revocation.
        await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = originalToken });

        // T2 must also be revoked now.
        var t2Resp = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = t2 });
        Assert.Equal(HttpStatusCode.Unauthorized, t2Resp.StatusCode);
    }

    [Fact]
    public async Task Refresh_NullToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = (string?)null });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST /api/auth/logout ────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ValidToken_Returns204()
    {
        await CreateUserAsync("LogoutOkUser");
        var loginResp = await LoginAsync("LogoutOkUser");
        var refreshToken = loginResp.GetProperty("refreshToken").GetString()!;

        var response = await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_TokenInvalidatedAfterLogout()
    {
        await CreateUserAsync("LogoutInvalidUser");
        var loginResp = await LoginAsync("LogoutInvalidUser");
        var refreshToken = loginResp.GetProperty("refreshToken").GetString()!;

        await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });

        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
    }

    [Fact]
    public async Task Logout_UnknownToken_Returns204()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/logout",
            new { refreshToken = "this-token-does-not-exist" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── Full cycle ───────────────────────────────────────────────────────────

    [Fact]
    public async Task FullCycle_Login_Refresh_Logout_Returns401OnFinalRefresh()
    {
        await CreateUserAsync("CycleUser");

        // Login.
        var loginResp = await LoginAsync("CycleUser");
        var refreshToken = loginResp.GetProperty("refreshToken").GetString()!;

        // Refresh.
        using var refreshBody = JsonDocument.Parse(
            await (await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken }))
                .Content.ReadAsStringAsync());
        var newRefreshToken = refreshBody.RootElement.GetProperty("refreshToken").GetString()!;

        // Logout with the new token.
        var logoutResp = await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = newRefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logoutResp.StatusCode);

        // Any subsequent refresh must be 401.
        var finalResp = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = newRefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, finalResp.StatusCode);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<JsonElement> LoginAsync(string userName, string password = "correct-horse")
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        resp.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }
}
