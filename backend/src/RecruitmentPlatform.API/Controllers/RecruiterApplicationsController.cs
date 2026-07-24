using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Recruiter;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>Recruiter application pipeline: review, status changes and AI ranking.</summary>
[Authorize(Roles = RoleNames.Recruiter)]
[Route("api/recruiter/applications")]
public class RecruiterApplicationsController : ApiControllerBase
{
    private readonly IRecruiterApplicationService _service;

    public RecruiterApplicationsController(IRecruiterApplicationService service) => _service = service;

    /// <summary>Lists applications to one of the recruiter's jobs, ranked best-match first.</summary>
    [HttpGet("by-job/{jobId:guid}")]
    [ProducesResponseType(typeof(PagedResult<RecruiterApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<RecruiterApplicationDto>>> GetForJob(Guid jobId, [FromQuery] RecruiterApplicationQuery query, CancellationToken cancellationToken)
        => Ok(await _service.GetForJobAsync(CurrentUserId, jobId, query, cancellationToken));

    /// <summary>Returns a single application (must belong to one of the recruiter's jobs).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RecruiterApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecruiterApplicationDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetAsync(CurrentUserId, id, cancellationToken));

    /// <summary>Downloads a candidate's resume for an application on one of the recruiter's jobs.</summary>
    [HttpGet("{id:guid}/resumes/{resumeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadResume(Guid id, Guid resumeId, CancellationToken cancellationToken)
    {
        var (content, contentType, fileName) = await _service.DownloadCandidateResumeAsync(CurrentUserId, id, resumeId, cancellationToken);
        return File(content, contentType, fileName);
    }

    /// <summary>Updates an application's status (shortlist, reject, advance) with optional notes.</summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(RecruiterApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecruiterApplicationDto>> UpdateStatus(Guid id, UpdateApplicationStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateStatusAsync(CurrentUserId, id, request, cancellationToken));

    /// <summary>Computes AI match scores for all applications to a job and returns them ranked.</summary>
    [HttpPost("by-job/{jobId:guid}/rank")]
    [ProducesResponseType(typeof(IReadOnlyList<RecruiterApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecruiterApplicationDto>>> Rank(Guid jobId, CancellationToken cancellationToken)
        => Ok(await _service.RankAsync(CurrentUserId, jobId, cancellationToken));
}
