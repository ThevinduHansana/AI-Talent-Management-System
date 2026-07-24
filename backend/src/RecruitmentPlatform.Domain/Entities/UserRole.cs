namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// Join entity mapping users to roles (many-to-many).
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
