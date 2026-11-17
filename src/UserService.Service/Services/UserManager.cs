using UserService.Domain.DTOs;
using UserService.Repository.Interfaces;
using UserService.Service.Interfaces;
using UserService.Service.Mappings;

namespace UserService.Service.Services;

public class UserManager(IUserRepository repository, ITokenService tokenService) : IUserManager
{
    public async Task<AuthResponse> RegisterAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await repository.ExistsByUsernameAsync(request.Username, ct))
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");

        if (await repository.ExistsByEmailAsync(request.Email, ct))
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var entity = await repository.AddAsync(request.ToEntity(), ct);
        var userResponse = entity.ToResponse();
        return new AuthResponse(tokenService.GenerateToken(userResponse), userResponse.Id, userResponse.Username);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await repository.FindByUsernameAsync(request.Username, ct)
            ?? throw new UnauthorizedAccessException("Invalid username or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        var userResponse = user.ToResponse();
        return new AuthResponse(tokenService.GenerateToken(userResponse), userResponse.Id, userResponse.Username);
    }
}
