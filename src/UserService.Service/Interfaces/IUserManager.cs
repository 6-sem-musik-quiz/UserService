using UserService.Domain.DTOs;

namespace UserService.Service.Interfaces;

public interface IUserManager
{
    Task<AuthResponse> RegisterAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
