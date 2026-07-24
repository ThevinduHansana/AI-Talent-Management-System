using RecruitmentPlatform.Application.DTOs.Ai;

namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>
/// AI-assisted candidate features. The Claude-backed implementation is used when an Anthropic
/// API key is configured; otherwise a deterministic keyword/heuristic implementation serves the
/// same contract, so callers never change.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Parses a candidate's resume, extracts skills, stores the parsed text and returns an
    /// analysis. When <paramref name="autoAddSkills"/> is true, newly detected skills are added
    /// to the candidate's profile.
    /// </summary>
    Task<ResumeAnalysisResultDto> AnalyzeResumeAsync(Guid userId, Guid resumeId, bool autoAddSkills, CancellationToken cancellationToken = default);

    /// <summary>Recommends open jobs for a candidate, ranked by AI match score.</summary>
    Task<IReadOnlyList<JobRecommendationDto>> RecommendJobsAsync(Guid userId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates constructive feedback on one of the candidate's own applications: how their
    /// profile lines up with the role, and what to strengthen.
    /// </summary>
    Task<ApplicationFeedbackDto> GenerateApplicationFeedbackAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);
}
