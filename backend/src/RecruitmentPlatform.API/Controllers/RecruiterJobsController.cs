using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Jobs;
using RecruitmentPlatform.Application.DTOs.Recruiter;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>Recruiter job management (create/edit/delete/publish the recruiter's own jobs).</summary>
[Authorize(Roles = RoleNames.Recruiter)]
[Route("api/recruiter/jobs")]
public class RecruiterJobsController : ApiControllerBase
{
    private readonly IRecruiterJobService _service;

    public RecruiterJobsController(IRecruiterJobService service) => _service = service;

    /// <summary>Lists the recruiter's jobs with optional status filter and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RecruiterJobDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RecruiterJobDto>>> GetMine([FromQuery] RecruiterJobQuery query, CancellationToken cancellationToken)
        => Ok(await _service.GetMyJobsAsync(CurrentUserId, query, cancellationToken));

    /// <summary>Returns full details of one of the recruiter's jobs.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetMyJobAsync(CurrentUserId, id, cancellationToken));

    /// <summary>Creates a job (draft or published depending on the supplied status).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RecruiterJobDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecruiterJobDto>> Create(SaveJobRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Updates one of the recruiter's jobs, including its required skills.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RecruiterJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecruiterJobDto>> Update(Guid id, SaveJobRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(CurrentUserId, id, request, cancellationToken));

    /// <summary>Deletes one of the recruiter's jobs.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}
