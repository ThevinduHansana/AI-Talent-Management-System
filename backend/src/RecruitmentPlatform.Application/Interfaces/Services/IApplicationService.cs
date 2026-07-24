using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Applications;

namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>
/// Candidate-facing application and saved-job operations.
/// </summary>
public interface IApplicationService
{
    Task<ApplicationDto> ApplyAsync(Guid userId, ApplyToJobRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<ApplicationDto>> GetMyApplicationsAsync(Guid userId, ApplicationQuery query, CancellationToken cancellationToken = default);

    Task<ApplicationDto> GetApplicationAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);

    Task WithdrawAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);

    // Saved jobs
    Task<SavedJobDto> SaveJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);
    Task UnsaveJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedJobDto>> GetSavedJobsAsync(Guid userId, CancellationToken cancellationToken = default);
}
