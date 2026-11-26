using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace UserService.Api.Tests;

public class UsersEndpointTests(UserServiceApiFactory factory) : IClassFixture<UserServiceApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_ValidUser_Returns201WithLocationAndDtoWithoutPassword()
    {
        const string password = "correct-horse";

        var response = await _client.PostAsJsonAsync("/api/users", new { userName = "Bob_1", password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(password, raw);
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);

        using var body = JsonDocument.Parse(raw);
        var root = body.RootElement;
        var id = root.GetProperty("id").GetInt32();
        Assert.True(id > 0);
        Assert.Equal("Bob_1", root.GetProperty("userName").GetString());
        Assert.Equal("Player", root.GetProperty("role").GetString());
        Assert.Equal(3, root.EnumerateObject().Count()); // exactly id, userName, role

        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/users/{id}", response.Headers.Location!.AbsolutePath);

        // The Location URL resolves to the created user.
        var get = await _client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidInput_Returns400ProblemDetailsWithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { userName = "a!", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
        var errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("userName", out _));
        Assert.True(errors.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Post_DuplicateUserNameDifferentCase_Returns409ProblemDetails()
    {
        var first = await _client.PostAsJsonAsync("/api/users", new { userName = "Alice", password = "password-123" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/users", new { userName = "alice", password = "password-456" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal(409, body.RootElement.GetProperty("status").GetInt32());
    }

    // ── GET /api/users/me ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_WithValidToken_Returns200WithOwnUserData()
    {
        var client = factory.CreateClient();
        var token = await CreateUserAndLoginAsync(client, "MeUser", "correct-horse-me");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal("MeUser", root.GetProperty("userName").GetString());
        Assert.Equal("Player", root.GetProperty("role").GetString());
        Assert.True(root.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var resp = await _client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithInvalidToken_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.token");

        var resp = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── GET /api/users?ids= ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIds_WithValidToken_Returns200WithMatchingUsers()
    {
        var client = factory.CreateClient();

        var id1 = await CreateUserAndGetIdAsync(client, "IdsUser1", "correct-horse-1");
        var id2 = await CreateUserAndGetIdAsync(client, "IdsUser2", "correct-horse-2");
        var token = await LoginAsync(client, "IdsUser1", "correct-horse-1");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.GetAsync($"/api/users?ids={id1},{id2}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var list = body.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, list.Count);
        Assert.True(list.All(u => u.TryGetProperty("id", out _) && u.TryGetProperty("userName", out _)));
        Assert.DoesNotContain(list, u => u.TryGetProperty("role", out _)); // role must not be in summary
    }

    [Fact]
    public async Task GetByIds_UnknownIdsSilentlySkipped_Returns200WithSubset()
    {
        var client = factory.CreateClient();
        var id1 = await CreateUserAndGetIdAsync(client, "SubsetUser1", "correct-horse-s1");
        var token = await LoginAsync(client, "SubsetUser1", "correct-horse-s1");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.GetAsync($"/api/users?ids={id1},999999");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var list = body.RootElement.EnumerateArray().ToList();
        Assert.Single(list);
        Assert.Equal(id1, list[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task GetByIds_WithoutToken_Returns401()
    {
        var resp = await _client.GetAsync("/api/users?ids=1,2,3");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetByIds_MoreThan100Ids_Returns400()
    {
        var client = factory.CreateClient();
        var token = await CreateUserAndLoginAsync(client, "LimitUser", "correct-horse-limit");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ids = string.Join(",", Enumerable.Range(1, 101));
        var resp = await client.GetAsync($"/api/users?ids={ids}");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetByIds_ExactlyOneHundredIds_Returns200()
    {
        var client = factory.CreateClient();
        var token = await CreateUserAndLoginAsync(client, "BoundaryUser", "correct-horse-boundary");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ids = string.Join(",", Enumerable.Range(1, 100));
        var resp = await client.GetAsync($"/api/users?ids={ids}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<string> CreateUserAndLoginAsync(HttpClient client, string userName, string password)
    {
        var createResp = await client.PostAsJsonAsync("/api/users", new { userName, password });
        createResp.EnsureSuccessStatusCode();
        return await LoginAsync(client, userName, password);
    }

    private static async Task<int> CreateUserAndGetIdAsync(HttpClient client, string userName, string password)
    {
        var createResp = await client.PostAsJsonAsync("/api/users", new { userName, password });
        createResp.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        loginResp.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }
}
