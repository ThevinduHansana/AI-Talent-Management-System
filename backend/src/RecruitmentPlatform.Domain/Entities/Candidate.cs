using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// The job-seeker profile attached to a user with the Candidate role.
/// </summary>
public class Candidate : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string? Headline { get; set; }

    public string? Summary { get; set; }

    public string? Location { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public Gender Gender { get; set; } = Gender.NotSpecified;

    public string? LinkedInUrl { get; set; }

    public string? PortfolioUrl { get; set; }

    public string? CurrentPosition { get; set; }

    public int YearsOfExperience { get; set; }

    public decimal? ExpectedSalary { get; set; }

    public string? PreferredCurrency { get; set; }

    public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.Available;

    // Navigation
    public ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();

    public ICollection<Education> Educations { get; set; } = new List<Education>();

    public ICollection<Experience> Experiences { get; set; } = new List<Experience>();

    public ICollection<Resume> Resumes { get; set; } = new List<Resume>();

    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();

    public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
}
