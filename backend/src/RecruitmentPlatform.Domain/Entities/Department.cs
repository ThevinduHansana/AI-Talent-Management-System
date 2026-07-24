using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A department within an organization that owns job openings.
/// </summary>
public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
