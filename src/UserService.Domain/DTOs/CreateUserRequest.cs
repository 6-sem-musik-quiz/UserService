namespace UserService.Domain.DTOs;

// Nullable so missing fields reach the service validation and produce field errors there.
public record CreateUserRequest(string? UserName, string? Password);
