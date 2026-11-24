namespace UserService.Domain.DTOs;

// Nullable so missing fields reach the service and return 401 (not 400).
public record LoginRequest(string? UserName, string? Password);
