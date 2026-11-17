using System.ComponentModel.DataAnnotations;

namespace UserService.Domain.DTOs;

public class CreateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; init; } = string.Empty;
}
