using UserService.Domain.DTOs;
using UserService.Domain.Entities;
using UserService.Repository.Interfaces;
using UserService.Service.Interfaces;
using UserService.Service.Services;
using NSubstitute;

namespace UserService.Service.Tests;

public class UserManagerTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly UserManager _sut;

    public UserManagerTests()
    {
        _sut = new UserManager(_repository, _tokenService);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsAuthResponse_WhenSuccessful()
    {
        _repository.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<User>());
        _tokenService.GenerateToken(Arg.Any<UserResponse>()).Returns("test-token");

        var result = await _sut.RegisterAsync(new CreateUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123"
        });

        Assert.Equal("test-token", result.Token);
        Assert.Equal("testuser", result.Username);
        Assert.NotEqual(Guid.Empty, result.UserId);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsInvalidOperationException_WhenUsernameIsTaken()
    {
        _repository.ExistsByUsernameAsync("testuser", Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(new CreateUserRequest
            {
                Username = "testuser",
                Email = "new@example.com",
                Password = "password123"
            }));

        Assert.Contains("testuser", ex.Message);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_ThrowsInvalidOperationException_WhenEmailIsTaken()
    {
        _repository.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsByEmailAsync("taken@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(new CreateUserRequest
            {
                Username = "newuser",
                Email = "taken@example.com",
                Password = "password123"
            }));

        Assert.Contains("taken@example.com", ex.Message);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_ReturnsAuthResponse_WhenCredentialsAreValid()
    {
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _repository.FindByUsernameAsync("testuser", Arg.Any<CancellationToken>()).Returns(user);
        _tokenService.GenerateToken(Arg.Any<UserResponse>()).Returns("test-token");

        var result = await _sut.LoginAsync(new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        });

        Assert.Equal("test-token", result.Token);
        Assert.Equal("testuser", result.Username);
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorizedAccessException_WhenPasswordIsWrong()
    {
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword")
        };
        _repository.FindByUsernameAsync("testuser", Arg.Any<CancellationToken>()).Returns(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginRequest
            {
                Username = "testuser",
                Password = "wrongpassword"
            }));
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorizedAccessException_WhenUserNotFound()
    {
        _repository.FindByUsernameAsync("unknown", Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginRequest
            {
                Username = "unknown",
                Password = "password123"
            }));
    }
}
