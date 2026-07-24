using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// The hiring-manager profile attached to a user with the HiringManager role.
/// </summary>
public class HiringManager : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid? OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public string? JobTitle { get; set; }

    public ICollection<CandidateEvaluation> Evaluations { get; set; } = new List<CandidateEvaluation>();
}
