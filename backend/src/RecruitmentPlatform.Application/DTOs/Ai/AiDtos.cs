using RecruitmentPlatform.Application.DTOs.Jobs;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.DTOs.Ai;

/// <summary>
/// Identifies which engine produced a result so clients can label AI output honestly.
/// </summary>
public static class AiSource
{
    public const string Model = "claude";
    public const string Heuristic = "heuristic";
}

public record ResumeAnalysisResultDto(
    Guid ResumeId,
    string FileName,
    int WordCount,
    IReadOnlyList<string> DetectedSkills,
    IReadOnlyList<string> SkillsAddedToProfile,
    IReadOnlyList<string> DetectedSections,
    int CompletenessScore,
    IReadOnlyList<string> Insights,
    string? Summary = null,
    IReadOnlyList<string>? Strengths = null,
    IReadOnlyList<string>? Gaps = null,
    IReadOnlyList<string>? SuggestedRoles = null,
    string Source = AiSource.Heuristic);

public record JobRecommendationDto(
    JobListItemDto Job,
    double MatchScore,
    IReadOnlyList<string> MatchingSkills,
    string? Rationale = null,
    string Source = AiSource.Heuristic);

/// <summary>Automated, candidate-facing feedback on a submitted application.</summary>
public record ApplicationFeedbackDto(
    Guid ApplicationId,
    Guid JobId,
    string JobTitle,
    string OrganizationName,
    ApplicationStatus Status,
    double MatchScore,
    string Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> Recommendations,
    string Source);
