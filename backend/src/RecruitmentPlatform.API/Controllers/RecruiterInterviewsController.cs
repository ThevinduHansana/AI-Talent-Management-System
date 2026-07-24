using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.DTOs.Recruiter;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>Interview scheduling for a recruiter's applications.</summary>
[Authorize(Roles = RoleNames.Recruiter)]
[Route("api/recruiter/interviews")]
public class RecruiterInterviewsController : ApiControllerBase
{
    private readonly IInterviewService _service;

    public RecruiterInterviewsController(IInterviewService service) => _service = service;

    /// <summary>Schedules an interview for an application and syncs it to the calendar.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(InterviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InterviewDto>> Schedule(ScheduleInterviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ScheduleAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Returns the recruiter's upcoming scheduled interviews.</summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InterviewDto>>> Upcoming(CancellationToken cancellationToken)
        => Ok(await _service.GetUpcomingAsync(CurrentUserId, cancellationToken));

    /// <summary>Lists all interviews for a given job.</summary>
    [HttpGet("by-job/{jobId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InterviewDto>>> ForJob(Guid jobId, CancellationToken cancellationToken)
        => Ok(await _service.GetForJobAsync(CurrentUserId, jobId, cancellationToken));

    /// <summary>Cancels a scheduled interview.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _service.CancelAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}
