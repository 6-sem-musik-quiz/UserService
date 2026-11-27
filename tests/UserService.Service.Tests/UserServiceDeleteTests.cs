using Microsoft.AspNetCore.Identity;
using NSubstitute;
using UserService.Domain.Entities;
using UserService.Repository.Interfaces;
using UserService.Service.Exceptions;
using UserService.Service.Interfaces;
using Sut = UserService.Service.Services.UserService;

namespace UserService.Service.Tests;

// SCRUM-197: only the load-test prefix may be deleted.
public class UserServiceDeleteTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly Sut _sut;

    public UserServiceDeleteTests() =>
        _sut = new Sut(_repository, new PasswordHasher<User>(), Substitute.For<ITokenService>());

    [Fact]
    public async Task DeleteLoadBotsAsync_WithLoadBotPrefix_DeletesAndReturnsCount()
    {
        _repository.DeleteByUserNamePrefixAsync("loadbot_", Arg.Any<CancellationToken>()).Returns(3);

        var deleted = await _sut.DeleteLoadBotsAsync("loadbot_");

        Assert.Equal(3, deleted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("loadbot")]
    [InlineData("LOADBOT_")]
    public async Task DeleteLoadBotsAsync_WithOtherPrefix_ThrowsAndDeletesNothing(string prefix)
    {
        await Assert.ThrowsAsync<UserValidationException>(() => _sut.DeleteLoadBotsAsync(prefix));

        await _repository.DidNotReceiveWithAnyArgs().DeleteByUserNamePrefixAsync(default!, default);
    }
}
