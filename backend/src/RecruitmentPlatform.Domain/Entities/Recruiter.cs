using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// The recruiter profile attached to a user with the Recruiter role.
/// </summary>
public class Recruiter : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid? OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public string? JobTitle { get; set; }

    public string? Bio { get; set; }

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
