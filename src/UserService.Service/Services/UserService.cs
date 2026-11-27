using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using UserService.Domain.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Repository.Interfaces;
using UserService.Service.Exceptions;
using UserService.Service.Interfaces;
using UserService.Service.Mappings;
using UserService.Service.Metrics;

namespace UserService.Service.Services;

public partial class UserService(
    IUserRepository repository,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    UserMetrics? metrics = null) : IUserService // SCRUM-173: counts logins; null in tests that do not look
{
    public const int UserNameMinLength = 3;
    public const int UserNameMaxLength = 20;
    public const int PasswordMinLength = 8;

    [GeneratedRegex("^[A-Za-z0-9_]{3,20}$")]
    private static partial Regex UserNamePattern();

    // SCRUM-197: the one prefix the admin cleanup may delete (the k6 bots are named loadbot_N).
    public const string LoadBotPrefix = "loadbot_";

    public async Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        (await repository.GetByIdAsync(id, ct))?.ToDto();

    public Task<int> DeleteLoadBotsAsync(string prefix, CancellationToken ct = default)
    {
        if (!string.Equals(prefix, LoadBotPrefix, StringComparison.Ordinal))
            throw new UserValidationException(new Dictionary<string, string[]>
            {
                ["prefix"] = [$"Only the load-test prefix '{LoadBotPrefix}' can be deleted."]
            });
        return repository.DeleteByUserNamePrefixAsync(LoadBotPrefix, ct);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        Validate(request);

        var userName = request.UserName!;
        if (await repository.ExistsByUserNameAsync(userName, ct))
            throw new DuplicateUserNameException(userName);

        var user = new User { UserName = userName, Role = UserRole.Player };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);

        var created = await repository.AddAsync(user, ct);
        return created.ToDto();
    }

    public async Task<IEnumerable<UserSummaryDto>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var users = await repository.GetByIdsAsync(ids, ct);
        return users.Select(u => u.ToSummaryDto());
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = request.UserName is not null
            ? await repository.GetByUserNameAsync(request.UserName, ct)
            : null;

        if (user is null
            || passwordHasher.VerifyHashedPassword(
                   user, user.PasswordHash, request.Password ?? string.Empty)
               == PasswordVerificationResult.Failed)
        {
            metrics?.Login(success: false);
            throw new InvalidCredentialsException();
        }

        metrics?.Login(success: true);
        var (token, expiresAt) = tokenService.CreateToken(user);
        return new LoginResponse(token, expiresAt, user.ToDto());
    }

    private static void Validate(CreateUserRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(request.UserName) || !UserNamePattern().IsMatch(request.UserName))
            errors["userName"] =
            [
                $"User name must be {UserNameMinLength}-{UserNameMaxLength} characters and contain only letters a-z, A-Z, digits and '_'."
            ];

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < PasswordMinLength)
            errors["password"] = [$"Password must be at least {PasswordMinLength} characters."];

        if (errors.Count > 0)
            throw new UserValidationException(errors);
    }
}
