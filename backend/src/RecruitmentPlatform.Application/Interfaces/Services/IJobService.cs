using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Jobs;

namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>
/// Read access to the job board used by candidates and the public site.
/// </summary>
public interface IJobService
{
    Task<PagedResult<JobListItemDto>> SearchAsync(JobQuery query, CancellationToken cancellationToken = default);

    Task<JobDetailDto> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
}
