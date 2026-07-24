using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A granular permission (e.g. "jobs.create") that can be granted to roles.
/// </summary>
public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Category { get; set; } = "General";

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
