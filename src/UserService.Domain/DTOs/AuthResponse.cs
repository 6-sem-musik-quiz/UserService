namespace UserService.Domain.DTOs;

public record AuthResponse(string Token, Guid UserId, string Username);
