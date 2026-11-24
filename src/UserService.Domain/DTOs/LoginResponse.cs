namespace UserService.Domain.DTOs;

public record LoginResponse(string Token, DateTime ExpiresAt, UserDto User);
