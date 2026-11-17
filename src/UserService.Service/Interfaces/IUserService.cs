using UserService.Domain.DTOs;

namespace UserService.Service.Interfaces;

public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
}
