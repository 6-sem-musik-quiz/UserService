using UserService.Domain.DTOs;

namespace UserService.Service.Interfaces;

public interface ITokenService
{
    string GenerateToken(UserResponse user);
}
