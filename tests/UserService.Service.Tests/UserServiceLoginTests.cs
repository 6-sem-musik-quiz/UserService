using Microsoft.AspNetCore.Identity;
using NSubstitute;
using UserService.Domain.DTOs;
using UserService.Domain.Entities;
using UserService.Repository.Interfaces;
using UserService.Service.Exceptions;
using UserService.Service.Interfaces;
using Sut = UserService.Service.Services.UserService;

namespace UserService.Service.Tests;

public class UserServiceLoginTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly Sut _sut;

    private const string ValidPassword = "correct-horse-battery";
    private const string FakeToken = "fake.jwt.token";

    public UserServiceLoginTests()
    {
        _tokenService.CreateToken(Arg.Any<User>())
            .Returns((FakeToken, DateTime.UtcNow.AddHours(12)));

        _sut = new Sut(_repository, _hasher, _tokenService);
    }

    private User CreateUserWithHash(string userName = "alice") =>
        new User
        {
            Id = 1,
            UserName = userName,
            Role = UserRole.Player,
            PasswordHash = _hasher.HashPassword(null!, ValidPassword)
        };

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsTokenAndUserDto()
    {
        var user = CreateUserWithHash();
        _repository.GetByUserNameAsync("alice", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.LoginAsync(new LoginRequest("alice", ValidPassword));

        Assert.Equal(FakeToken, result.Token);
        Assert.Equal(new UserDto(1, "alice", UserRole.Player), result.User);
        _tokenService.Received(1).CreateToken(user);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentials()
    {
        var user = CreateUserWithHash();
        _repository.GetByUserNameAsync("alice", Arg.Any<CancellationToken>()).Returns(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.LoginAsync(new LoginRequest("alice", "wrong-password")));

        _tokenService.DidNotReceive().CreateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task LoginAsync_UnknownUser_ThrowsInvalidCredentials()
    {
        _repository.GetByUserNameAsync("nobody", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.LoginAsync(new LoginRequest("nobody", ValidPassword)));

        _tokenService.DidNotReceive().CreateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_SameExceptionAsUnknownUser()
    {
        var user = CreateUserWithHash();
        _repository.GetByUserNameAsync("alice", Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetByUserNameAsync("nobody", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var exWrong = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.LoginAsync(new LoginRequest("alice", "wrong")));
        var exUnknown = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.LoginAsync(new LoginRequest("nobody", ValidPassword)));

        Assert.Equal(exWrong.Message, exUnknown.Message);
    }
}
