using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using UserService.Domain.DTOs;
using UserService.Domain.Entities;
using UserService.Repository.Interfaces;
using UserService.Service.Exceptions;
using UserService.Service.Interfaces;
using UserService.Service.Metrics;
using Sut = UserService.Service.Services.UserService;

namespace UserService.Service.Tests;

// SCRUM-173: slopster_logins_total counts every login attempt by result.
public class UserMetricsTests : IDisposable
{
    private const string Password = "correct-horse-battery";
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();
    private readonly UserMetrics _metrics = new();
    private readonly MeterListener _listener = new();
    private readonly List<string?> _results = [];
    private readonly Sut _sut;

    public UserMetricsTests()
    {
        var tokens = Substitute.For<ITokenService>();
        tokens.CreateToken(Arg.Any<User>()).Returns(("token", DateTime.UtcNow.AddHours(1)));
        _sut = new Sut(_repository, _hasher, tokens, _metrics);
        _repository.GetByUserNameAsync("alice", Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1, UserName = "alice", Role = UserRole.Player, PasswordHash = _hasher.HashPassword(null!, Password) });

        // Only this test's meter: other tests may create their own at the same time.
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter == _metrics.Meter) listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            Assert.Equal(UserMetrics.LoginsName, instrument.Name);
            foreach (var tag in tags)
                if (tag.Key == "result") _results.Add(tag.Value as string);
        });
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _metrics.Dispose();
    }

    [Fact]
    public async Task Logins_AreCountedBySuccessAndFailure()
    {
        await _sut.LoginAsync(new LoginRequest("alice", Password));
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.LoginAsync(new LoginRequest("alice", "wrong-password")));
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.LoginAsync(new LoginRequest("nobody", Password)));

        Assert.Equal(["success", "failed", "failed"], _results);
    }
}
