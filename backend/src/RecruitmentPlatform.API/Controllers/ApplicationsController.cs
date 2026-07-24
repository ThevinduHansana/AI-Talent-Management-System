using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Applications;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>Candidate job applications and saved jobs.</summary>
[Authorize(Roles = RoleNames.Candidate)]
public class ApplicationsController : ApiControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService) => _applicationService = applicationService;

    /// <summary>Submits an application to a job on behalf of the authenticated candidate.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationDto>> Apply(ApplyToJobRequest request, CancellationToken cancellationToken)
    {
        var result = await _applicationService.ApplyAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Lists the authenticated candidate's applications with optional status filter.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ApplicationDto>>> GetMine([FromQuery] ApplicationQuery query, CancellationToken cancellationToken)
        => Ok(await _applicationService.GetMyApplicationsAsync(CurrentUserId, query, cancellationToken));

    /// <summary>Returns a single application owned by the authenticated candidate.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _applicationService.GetApplicationAsync(CurrentUserId, id, cancellationToken));

    /// <summary>Withdraws an active application.</summary>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken cancellationToken)
    {
        await _applicationService.WithdrawAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }

    // ----- Saved jobs -----

    [HttpGet("saved")]
    [ProducesResponseType(typeof(IReadOnlyList<SavedJobDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SavedJobDto>>> GetSaved(CancellationToken cancellationToken)
        => Ok(await _applicationService.GetSavedJobsAsync(CurrentUserId, cancellationToken));

    [HttpPost("saved/{jobId:guid}")]
    [ProducesResponseType(typeof(SavedJobDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SavedJobDto>> SaveJob(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await _applicationService.SaveJobAsync(CurrentUserId, jobId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("saved/{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnsaveJob(Guid jobId, CancellationToken cancellationToken)
    {
        await _applicationService.UnsaveJobAsync(CurrentUserId, jobId, cancellationToken);
        return NoContent();
    }
}
