using UserService.Domain.DTOs;
using UserService.Repository.Interfaces;
using UserService.Service.Interfaces;
using UserService.Service.Mappings;

namespace UserService.Service.Services;

public class UserService(IUserRepository repository) : IUserService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await repository.ExistsByUsernameAsync(request.Username, ct))
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");

        if (await repository.ExistsByEmailAsync(request.Email, ct))
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var entity = await repository.AddAsync(request.ToEntity(), ct);
        return entity.ToResponse();
    }
}
