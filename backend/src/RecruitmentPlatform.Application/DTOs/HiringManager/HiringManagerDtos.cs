using RecruitmentPlatform.Application.DTOs.Candidates;
using RecruitmentPlatform.Application.DTOs.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.DTOs.HiringManager;

// ----- Review queue -----

public record ReviewCandidateDto(
    Guid ApplicationId,
    Guid JobId,
    string JobTitle,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail,
    string? Headline,
    ApplicationStatus Status,
    double? MatchScore,
    DateTime AppliedAt,
    bool HasEvaluation,
    int? OverallScore,
    int InterviewCount);

public class ReviewQueueQuery : PaginationQuery
{
    public ApplicationStatus? Status { get; set; }
    public Guid? JobId { get; set; }
}

// ----- Evaluation -----

public record EvaluationDto(
    Guid Id,
    Guid ApplicationId,
    int TechnicalScore,
    int CommunicationScore,
    int CultureFitScore,
    int OverallScore,
    string? Comments,
    EvaluationDecision Decision,
    string HiringManagerName,
    DateTime EvaluatedAt);

public record SubmitEvaluationRequest(
    Guid ApplicationId,
    int TechnicalScore,
    int CommunicationScore,
    int CultureFitScore,
    string? Comments);

// ----- Interview feedback -----

public record InterviewFeedbackDto(
    Guid Id,
    Guid InterviewScheduleId,
    int Rating,
    string? Strengths,
    string? Weaknesses,
    string? Comments,
    HiringRecommendation Recommendation,
    string InterviewerName,
    DateTime SubmittedAt);

public record SubmitInterviewFeedbackRequest(
    Guid InterviewScheduleId,
    int Rating,
    string? Strengths,
    string? Weaknesses,
    string? Comments,
    HiringRecommendation Recommendation);

public record InterviewSummaryDto(
    Guid Id,
    string Title,
    DateTime ScheduledAt,
    int DurationMinutes,
    InterviewMode Mode,
    InterviewStatus Status,
    IReadOnlyList<InterviewFeedbackDto> Feedback);

// ----- Candidate review detail -----

public record CandidateReviewDetailDto(
    Guid ApplicationId,
    Guid JobId,
    string JobTitle,
    ApplicationStatus Status,
    double? MatchScore,
    string? CoverLetter,
    DateTime AppliedAt,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail,
    string? Headline,
    string? Summary,
    string? Location,
    string? CurrentPosition,
    int YearsOfExperience,
    IReadOnlyList<CandidateSkillDto> Skills,
    IReadOnlyList<EducationDto> Education,
    IReadOnlyList<ExperienceDto> Experience,
    IReadOnlyList<ResumeDto> Resumes,
    IReadOnlyList<InterviewSummaryDto> Interviews,
    EvaluationDto? Evaluation);

// ----- Hiring decision -----

public record HiringDecisionRequest(string? Comments);

// ----- Dashboard -----

public record HiringManagerDashboardDto(
    int ToReview,
    int Evaluated,
    int Hired,
    int Rejected,
    int PendingDecision);
