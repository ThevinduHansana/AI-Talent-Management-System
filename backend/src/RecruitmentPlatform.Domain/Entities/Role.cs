using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// An application role (Candidate, Recruiter, HiringManager, Administrator) used for RBAC.
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
