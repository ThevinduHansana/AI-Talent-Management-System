using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A client organization for which recruitment is performed.
/// </summary>
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Industry { get; set; }

    public string? Website { get; set; }

    public string? Location { get; set; }

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Department> Departments { get; set; } = new List<Department>();

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
