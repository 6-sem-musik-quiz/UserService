using UserService.Domain.DTOs;
using UserService.Domain.Entities;

namespace UserService.Service.Mappings;

public static class UserMappings
{
    public static UserResponse ToResponse(this User entity) =>
        new(entity.Id, entity.Username, entity.Email, entity.CreatedAt);

    public static User ToEntity(this CreateUserRequest request) =>
        new()
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
}
