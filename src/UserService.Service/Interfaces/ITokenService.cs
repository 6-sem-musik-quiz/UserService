using UserService.Domain.Entities;

namespace UserService.Service.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}
