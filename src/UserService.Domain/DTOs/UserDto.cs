using UserService.Domain.Entities;

namespace UserService.Domain.DTOs;

// Public view of a user. Never contains the password or its hash.
public record UserDto(int Id, string UserName, UserRole Role);
