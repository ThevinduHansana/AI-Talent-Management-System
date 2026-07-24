using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A hiring manager's structured evaluation of a candidate for an application.
/// </summary>
public class CandidateEvaluation : BaseEntity
{
    public Guid ApplicationId { get; set; }

    public JobApplication Application { get; set; } = null!;

    public Guid HiringManagerId { get; set; }

    public HiringManager HiringManager { get; set; } = null!;

    /// <summary>Scores are on a 0-100 scale.</summary>
    public int TechnicalScore { get; set; }

    public int CommunicationScore { get; set; }

    public int CultureFitScore { get; set; }

    public int OverallScore { get; set; }

    public string? Comments { get; set; }

    public EvaluationDecision Decision { get; set; } = EvaluationDecision.Pending;

    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}
