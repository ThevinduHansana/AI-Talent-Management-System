using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A job opening posted by a recruiter on behalf of an organization/department.
/// </summary>
public class Job : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Responsibilities { get; set; }

    public string? Requirements { get; set; }

    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

    public ExperienceLevel ExperienceLevel { get; set; } = ExperienceLevel.Mid;

    public string? Location { get; set; }

    public bool IsRemote { get; set; }

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    public string Currency { get; set; } = "USD";

    public int Vacancies { get; set; } = 1;

    public JobStatus Status { get; set; } = JobStatus.Draft;

    public DateTime? PostedAt { get; set; }

    public DateTime? ClosingDate { get; set; }

    // Relationships
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public Guid RecruiterId { get; set; }

    public Recruiter Recruiter { get; set; } = null!;

    public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();

    public ICollection<SavedJob> SavedByCandidates { get; set; } = new List<SavedJob>();
}
