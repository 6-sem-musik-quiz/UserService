namespace UserService.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid FamilyId { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
