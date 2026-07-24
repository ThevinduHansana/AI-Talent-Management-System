using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Jobs;
using RecruitmentPlatform.Application.DTOs.Recruiter;

namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>
/// Recruiter job management. All operations are scoped to jobs owned by the recruiter resolved
/// from the authenticated user.
/// </summary>
public interface IRecruiterJobService
{
    Task<PagedResult<RecruiterJobDto>> GetMyJobsAsync(Guid userId, RecruiterJobQuery query, CancellationToken cancellationToken = default);
    Task<JobDetailDto> GetMyJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);
    Task<RecruiterJobDto> CreateAsync(Guid userId, SaveJobRequest request, CancellationToken cancellationToken = default);
    Task<RecruiterJobDto> UpdateAsync(Guid userId, Guid jobId, SaveJobRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Recruiter view of applications and the pipeline actions (status changes, notes, ranking).
/// </summary>
public interface IRecruiterApplicationService
{
    Task<PagedResult<RecruiterApplicationDto>> GetForJobAsync(Guid userId, Guid jobId, RecruiterApplicationQuery query, CancellationToken cancellationToken = default);
    Task<RecruiterApplicationDto> GetAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<RecruiterApplicationDto> UpdateStatusAsync(Guid userId, Guid applicationId, UpdateApplicationStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a candidate's resume for a pipeline application. Authorized only when the resume belongs
    /// to the candidate of an application on a job owned by the recruiter.
    /// </summary>
    Task<(Stream content, string contentType, string fileName)> DownloadCandidateResumeAsync(Guid userId, Guid applicationId, Guid resumeId, CancellationToken cancellationToken = default);

    /// <summary>Computes AI match scores for every application to a job and ranks them.</summary>
    Task<IReadOnlyList<RecruiterApplicationDto>> RankAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interview scheduling for a recruiter's applications, backed by the calendar abstraction.
/// </summary>
public interface IInterviewService
{
    Task<InterviewDto> ScheduleAsync(Guid userId, ScheduleInterviewRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterviewDto>> GetForJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterviewDto>> GetUpcomingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid userId, Guid interviewId, CancellationToken cancellationToken = default);
}
