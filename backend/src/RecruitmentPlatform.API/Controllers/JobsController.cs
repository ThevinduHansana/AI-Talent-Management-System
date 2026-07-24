using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Jobs;
using RecruitmentPlatform.Application.Interfaces.Services;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>Public job board: search open jobs and view details.</summary>
public class JobsController : ApiControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService) => _jobService = jobService;

    /// <summary>Searches open jobs with filtering, sorting and pagination.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<JobListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<JobListItemDto>>> Search([FromQuery] JobQuery query, CancellationToken cancellationToken)
        => Ok(await _jobService.SearchAsync(query, cancellationToken));

    /// <summary>Returns full details for a single job.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JobDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _jobService.GetByIdAsync(id, cancellationToken));
}
