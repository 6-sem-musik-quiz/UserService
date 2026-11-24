using Microsoft.AspNetCore.Identity;
using NSubstitute;
using UserService.Domain.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Repository.Interfaces;
using UserService.Service.Exceptions;
using UserService.Service.Interfaces;
using Sut = UserService.Service.Services.UserService;

namespace UserService.Service.Tests;

public class UserServiceTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly Sut _sut;

    public UserServiceTests()
    {
        _repository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var user = call.Arg<User>();
                user.Id = 42;
                return user;
            });
        _sut = new Sut(_repository, _hasher, _tokenService);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_HashesPasswordAndCreatesPlayer()
    {
        const string password = "s3cret-password";

        var result = await _sut.CreateAsync(new CreateUserRequest("Alice_01", password));

        Assert.Equal(new UserDto(42, "Alice_01", UserRole.Player), result);
        await _repository.Received(1).AddAsync(
            Arg.Is<User>(u =>
                u.UserName == "Alice_01"
                && u.Role == UserRole.Player
                && u.PasswordHash != password
                && !u.PasswordHash.Contains(password)
                && _hasher.VerifyHashedPassword(u, u.PasswordHash, password) == PasswordVerificationResult.Success),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]                    // too short
    [InlineData("abcdefghijklmnopqrstu")] // 21 chars, too long
    [InlineData("alice!")]                // illegal character
    [InlineData("al ice")]                // whitespace
    [InlineData("ålice")]                 // non-ASCII letter
    public async Task CreateAsync_InvalidUserName_ThrowsWithUserNameError(string? userName)
    {
        var ex = await Assert.ThrowsAsync<UserValidationException>(
            () => _sut.CreateAsync(new CreateUserRequest(userName, "long-enough-password")));

        Assert.Contains("userName", ex.Errors.Keys);
        Assert.DoesNotContain("password", ex.Errors.Keys);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")] // 7 chars
    public async Task CreateAsync_PasswordTooShort_ThrowsWithPasswordError(string? password)
    {
        var ex = await Assert.ThrowsAsync<UserValidationException>(
            () => _sut.CreateAsync(new CreateUserRequest("alice", password)));

        Assert.Contains("password", ex.Errors.Keys);
        Assert.DoesNotContain("userName", ex.Errors.Keys);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_PasswordOfExactlyMinLength_IsAccepted()
    {
        var result = await _sut.CreateAsync(new CreateUserRequest("abc", "12345678"));

        Assert.Equal("abc", result.UserName);
    }

    [Fact]
    public async Task CreateAsync_DuplicateWithDifferentCase_ThrowsDuplicate()
    {
        // The repository owns case-insensitive matching; the service must pass the name through
        // unchanged and refuse to create the user when it reports a match.
        _repository.ExistsByUserNameAsync("ALICE", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DuplicateUserNameException>(
            () => _sut.CreateAsync(new CreateUserRequest("ALICE", "long-enough-password")));

        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
