using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using UserService.Domain.Entities;
using UserService.Repository.Interfaces;
using UserService.Service.Exceptions;
using UserService.Service.Services;

namespace UserService.Service.Tests.Auth;

public class RefreshTokenServiceTests
{
    private readonly IRefreshTokenRepository _repository = Substitute.For<IRefreshTokenRepository>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();
    private readonly ILogger<RefreshTokenService> _logger = Substitute.For<ILogger<RefreshTokenService>>();
    private readonly RefreshTokenService _sut;

    private static readonly User TestUser = new() { Id = 42, UserName = "alice", Role = UserRole.Player };

    public RefreshTokenServiceTests()
    {
        _repository.AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var t = callInfo.Arg<RefreshToken>();
                t.Id = 1;
                // Attach a user so RotateAsync can return it via existing.User.
                if (t.UserId == TestUser.Id)
                    t.User = TestUser;
                return Task.FromResult(t);
            });

        _sut = new RefreshTokenService(_repository, _configuration, _logger);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsNonEmptyToken()
    {
        var (plainToken, expiresAt) = await _sut.CreateAsync(TestUser.Id);

        Assert.False(string.IsNullOrEmpty(plainToken));
        Assert.True(expiresAt > DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task CreateAsync_StoresHashedToken_NotPlaintext()
    {
        RefreshToken? stored = null;
        _repository.AddAsync(Arg.Do<RefreshToken>(t => stored = t), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<RefreshToken>()));

        var (plainToken, _) = await _sut.CreateAsync(TestUser.Id);

        Assert.NotNull(stored);
        Assert.NotEqual(plainToken, stored!.TokenHash);
        // SHA-256 hex is always 64 characters.
        Assert.Equal(64, stored.TokenHash.Length);
    }

    [Fact]
    public async Task CreateAsync_EachTokenHasUniqueFamilyId()
    {
        var familyIds = new List<Guid>();
        _repository.AddAsync(Arg.Do<RefreshToken>(t => familyIds.Add(t.FamilyId)), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<RefreshToken>()));

        await _sut.CreateAsync(TestUser.Id);
        await _sut.CreateAsync(TestUser.Id);

        Assert.Equal(2, familyIds.Distinct().Count());
    }

    // ── RotateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RotateAsync_ValidToken_ReturnsNewTokenAndUser()
    {
        var existingToken = MakeToken(isRevoked: false);
        _repository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingToken);

        var (newPlainToken, expiresAt, user) = await _sut.RotateAsync("original-plain-token");

        Assert.False(string.IsNullOrEmpty(newPlainToken));
        Assert.True(expiresAt > DateTime.UtcNow.AddDays(6));
        Assert.Equal(TestUser.Id, user.Id);
    }

    [Fact]
    public async Task RotateAsync_ValidToken_OldTokenIsRevoked()
    {
        var existingToken = MakeToken(isRevoked: false);
        _repository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingToken);

        await _sut.RotateAsync("original-plain-token");

        Assert.True(existingToken.IsRevoked);
        await _repository.Received(1).UpdateAsync(existingToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateAsync_ValidToken_NewTokenHasSameFamilyId()
    {
        var familyId = Guid.NewGuid();
        var existingToken = MakeToken(isRevoked: false, familyId: familyId);
        _repository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingToken);

        RefreshToken? newToken = null;
        _repository.AddAsync(Arg.Do<RefreshToken>(t => newToken = t), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<RefreshToken>()));

        await _sut.RotateAsync("original-plain-token");

        Assert.NotNull(newToken);
        Assert.Equal(familyId, newToken!.FamilyId);
    }

    [Fact]
    public async Task RotateAsync_UnknownToken_ThrowsInvalidRefreshTokenException()
    {
        _repository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            _sut.RotateAsync("unknown-token"));
    }

    [Fact]
    public async Task RotateAsync_ExpiredToken_ThrowsInvalidRefreshTokenException()
    {
        var expiredToken = MakeToken(isRevoked: false, expiresAt: DateTime.UtcNow.AddSeconds(-1));
        _repository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expiredToken);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            _sut.RotateAsync("expired-token"));
    }

    [Fact]
    public async Task RotateAsync_RevokedToken_RevokesFamily_ThrowsInvalidRefreshTokenException()
    {
        var revokedToken = MakeToken(isRevoked: true);
        _repository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(revokedToken);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            _sut.RotateAsync("reused-token"));

        await _repository.Received(1).RevokeByFamilyIdAsync(revokedToken.FamilyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateAsync_RevokedToken_LogsWarning()
    {
        var revokedToken = MakeToken(isRevoked: true);
        _repository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(revokedToken);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            _sut.RotateAsync("reused-token"));

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ── RevokeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAsync_ValidToken_RevokesToken()
    {
        var token = MakeToken(isRevoked: false);
        _repository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);

        await _sut.RevokeAsync("valid-token");

        Assert.True(token.IsRevoked);
        await _repository.Received(1).UpdateAsync(token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeAsync_AlreadyRevoked_DoesNotUpdate()
    {
        var token = MakeToken(isRevoked: true);
        _repository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);

        await _sut.RevokeAsync("already-revoked-token");

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeAsync_UnknownToken_DoesNotThrow()
    {
        _repository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var ex = await Record.ExceptionAsync(() => _sut.RevokeAsync("unknown-token"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task RevokeAsync_NullToken_DoesNotQueryRepository()
    {
        await _sut.RevokeAsync(null);

        await _repository.DidNotReceive().GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RefreshToken MakeToken(bool isRevoked, Guid? familyId = null, DateTime? expiresAt = null) =>
        new()
        {
            Id = 1,
            TokenHash = "some-hash",
            FamilyId = familyId ?? Guid.NewGuid(),
            IsRevoked = isRevoked,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            UserId = TestUser.Id,
            User = TestUser,
        };
}
