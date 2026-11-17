using UserService.Domain.DTOs;
using UserService.Domain.Entities;
using UserService.Repository.Interfaces;
using UserService.Service.Services;
using NSubstitute;
using UserServiceImpl = UserService.Service.Services.UserService;

namespace UserService.Service.Tests;

public class UserServiceTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly UserServiceImpl _sut;

    public UserServiceTests()
    {
        _sut = new UserServiceImpl(_repository);
    }

    [Fact]
    public async Task CreateAsync_ReturnsUserResponse_WhenUsernameAndEmailAreUnique()
    {
        _repository.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<User>());

        var result = await _sut.CreateAsync(new CreateUserRequest { Username = "testuser", Email = "test@example.com", Password = "password123" });

        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateAsync_ThrowsInvalidOperationException_WhenUsernameAlreadyExists()
    {
        _repository.ExistsByUsernameAsync("testuser", Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(new CreateUserRequest { Username = "testuser", Email = "new@example.com", Password = "password123" }));

        Assert.Contains("testuser", ex.Message);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ThrowsInvalidOperationException_WhenEmailAlreadyExists()
    {
        _repository.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsByEmailAsync("taken@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(new CreateUserRequest { Username = "newuser", Email = "taken@example.com", Password = "password123" }));

        Assert.Contains("taken@example.com", ex.Message);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
