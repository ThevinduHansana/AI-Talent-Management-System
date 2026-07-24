using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A candidate's application to a job, tracked through the hiring workflow.
/// (Named JobApplication to avoid colliding with the RecruitmentPlatform.Application namespace.)
/// </summary>
public class JobApplication : BaseEntity
{
    public Guid JobId { get; set; }

    public Job Job { get; set; } = null!;

    public Guid CandidateId { get; set; }

    public Candidate Candidate { get; set; } = null!;

    public Guid? ResumeId { get; set; }

    public Resume? Resume { get; set; }

    public string? CoverLetter { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

    /// <summary>AI-computed candidate/job fit score (0-100).</summary>
    public double? MatchScore { get; set; }

    /// <summary>Recruiter/AI ranking score used to order the shortlist (0-100).</summary>
    public double? RankScore { get; set; }

    public string? RecruiterNotes { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public DateTime? StatusChangedAt { get; set; }

    public ICollection<InterviewSchedule> Interviews { get; set; } = new List<InterviewSchedule>();

    public ICollection<CandidateEvaluation> Evaluations { get; set; } = new List<CandidateEvaluation>();
}
