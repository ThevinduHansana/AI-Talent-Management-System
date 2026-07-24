using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A work-experience record on a candidate's profile.
/// </summary>
public class Experience : BaseEntity
{
    public Guid CandidateId { get; set; }

    public Candidate Candidate { get; set; } = null!;

    public string Company { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Location { get; set; }

    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public string? Description { get; set; }
}
