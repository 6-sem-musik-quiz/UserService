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

public class UserServiceGetByIdsTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly Sut _sut;

    public UserServiceGetByIdsTests()
    {
        _sut = new Sut(_repository, _hasher, _tokenService);
    }

    [Fact]
    public async Task GetByIdsAsync_ValidIds_ReturnsMappedSummaries()
    {
        var users = new List<User>
        {
            new() { Id = 1, UserName = "Alice", Role = UserRole.Player, PasswordHash = "x" },
            new() { Id = 2, UserName = "Bob", Role = UserRole.Player, PasswordHash = "x" },
        };
        _repository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(users);

        var result = (await _sut.GetByIdsAsync([1, 2])).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == 1 && u.UserName == "Alice");
        Assert.Contains(result, u => u.Id == 2 && u.UserName == "Bob");
    }

    [Fact]
    public async Task GetByIdsAsync_SummaryDto_DoesNotExposeRole()
    {
        _repository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([new User { Id = 1, UserName = "Alice", Role = UserRole.Player, PasswordHash = "x" }]);

        var result = (await _sut.GetByIdsAsync([1])).Single();

        Assert.IsType<UserSummaryDto>(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Alice", result.UserName);
    }

    [Fact]
    public async Task GetByIdsAsync_EmptyList_ReturnsEmpty()
    {
        _repository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.GetByIdsAsync([]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdsAsync_SomeIdsNotFound_OnlyReturnsFoundUsers()
    {
        _repository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([new User { Id = 1, UserName = "Alice", Role = UserRole.Player, PasswordHash = "x" }]);

        var result = (await _sut.GetByIdsAsync([1, 999])).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetByIdsAsync_ExactlyOneHundredIds_DelegatesToRepository()
    {
        var ids = Enumerable.Range(1, 100).ToList();
        _repository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.GetByIdsAsync(ids);

        await _repository.Received(1).GetByIdsAsync(
            Arg.Is<IEnumerable<int>>(x => x.Count() == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdsAsync_OneHundredAndOneIds_DelegatesToRepository()
    {
        // The 100-ID limit is enforced at the controller level (HTTP 400), not in the service.
        // The service itself imposes no upper bound and passes all IDs to the repository.
        var ids = Enumerable.Range(1, 101).ToList();
        _repository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.GetByIdsAsync(ids);

        await _repository.Received(1).GetByIdsAsync(
            Arg.Is<IEnumerable<int>>(x => x.Count() == 101),
            Arg.Any<CancellationToken>());
    }
}
