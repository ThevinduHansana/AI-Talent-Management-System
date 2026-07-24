using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.HiringManager;

namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>
/// Hiring-manager operations, scoped to the manager's organization: reviewing candidates,
/// submitting evaluations and interview feedback, and making the final hiring decision.
/// </summary>
public interface IHiringManagerService
{
    Task<HiringManagerDashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PagedResult<ReviewCandidateDto>> GetReviewQueueAsync(Guid userId, ReviewQueueQuery query, CancellationToken cancellationToken = default);

    Task<CandidateReviewDetailDto> GetCandidateDetailAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a candidate's resume for review. Access is authorized only when the resume belongs to
    /// the candidate of an application within the hiring manager's organization.
    /// </summary>
    Task<(Stream content, string contentType, string fileName)> DownloadCandidateResumeAsync(Guid userId, Guid applicationId, Guid resumeId, CancellationToken cancellationToken = default);

    Task<EvaluationDto> SubmitEvaluationAsync(Guid userId, SubmitEvaluationRequest request, CancellationToken cancellationToken = default);

    Task<InterviewFeedbackDto> SubmitInterviewFeedbackAsync(Guid userId, SubmitInterviewFeedbackRequest request, CancellationToken cancellationToken = default);

    Task<EvaluationDto> ApproveHiringAsync(Guid userId, Guid applicationId, HiringDecisionRequest request, CancellationToken cancellationToken = default);

    Task<EvaluationDto> RejectHiringAsync(Guid userId, Guid applicationId, HiringDecisionRequest request, CancellationToken cancellationToken = default);
}
