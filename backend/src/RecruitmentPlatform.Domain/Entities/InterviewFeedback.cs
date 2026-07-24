using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// Feedback submitted by an interviewer after an interview.
/// </summary>
public class InterviewFeedback : BaseEntity
{
    public Guid InterviewScheduleId { get; set; }

    public InterviewSchedule InterviewSchedule { get; set; } = null!;

    public Guid InterviewerUserId { get; set; }

    public User InterviewerUser { get; set; } = null!;

    /// <summary>Overall rating (1-5).</summary>
    public int Rating { get; set; }

    public string? Strengths { get; set; }

    public string? Weaknesses { get; set; }

    public string? Comments { get; set; }

    public HiringRecommendation Recommendation { get; set; } = HiringRecommendation.Yes;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
