using UserService.Domain.DTOs;

namespace UserService.Service.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default);

    // Throws UserValidationException on invalid input and DuplicateUserNameException
    // if the name is taken (case-insensitive). New users always get Role = Player.
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);

    // Throws InvalidCredentialsException for unknown user OR wrong password (same message).
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    // Returns UserSummaryDto for each id that exists; unknown ids are silently skipped.
    Task<IEnumerable<UserSummaryDto>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
}
