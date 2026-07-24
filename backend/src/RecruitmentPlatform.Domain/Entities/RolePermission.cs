namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// Join entity mapping roles to permissions (many-to-many).
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public Guid PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
