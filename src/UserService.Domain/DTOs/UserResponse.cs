namespace UserService.Domain.DTOs;

public record UserResponse(Guid Id, string Username, string Email, DateTime CreatedAt);
