using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using UserService.Domain.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Repository.Interfaces;
using UserService.Service.Exceptions;
using UserService.Service.Interfaces;
using UserService.Service.Mappings;

namespace UserService.Service.Services;

public partial class UserService(IUserRepository repository, IPasswordHasher<User> passwordHasher) : IUserService
{
    public const int UserNameMinLength = 3;
    public const int UserNameMaxLength = 20;
    public const int PasswordMinLength = 8;

    [GeneratedRegex("^[A-Za-z0-9_]{3,20}$")]
    private static partial Regex UserNamePattern();

    public async Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        (await repository.GetByIdAsync(id, ct))?.ToDto();

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
