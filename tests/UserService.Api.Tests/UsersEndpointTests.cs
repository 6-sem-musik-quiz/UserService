using System.Net;
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
}
