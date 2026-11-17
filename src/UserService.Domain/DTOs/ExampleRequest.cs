using System.ComponentModel.DataAnnotations;

namespace UserService.Domain.DTOs;

public class ExampleRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}
