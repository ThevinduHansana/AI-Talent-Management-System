using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.DTOs.Interviews;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>
/// Interview scheduling with calendar integration. Creating an interview emails the candidate an
/// invitation containing "Add to Google Calendar" and "Add to Outlook Calendar" links plus an
/// attached .ics file, so reminders are handled by the candidate's own calendar.
/// </summary>
[Authorize]
[Route("api/interviews")]
public class InterviewsController : ApiControllerBase
{
    private readonly IInterviewSchedulingService _service;

    public InterviewsController(IInterviewSchedulingService service) => _service = service;

    /// <summary>Schedules an interview and emails the calendar invitation to the candidate.</summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/interviews
    ///     {
    ///       "applicationId": "b8471934-4ad8-4861-a638-c586bf2c763d",
    ///       "title": "Technical Interview",
    ///       "interviewDate": "2026-08-01T14:00:00Z",
    ///       "durationMinutes": 60,
    ///       "mode": "Video",
    ///       "location": null,
    ///       "meetingLink": "https://meet.google.com/abc-defg-hij",
    ///       "interviewerUserId": null,
    ///       "notes": "Focus on system design."
    ///     }
    /// </remarks>
    /// <response code="201">Interview scheduled; calendar links returned and invitation sent.</response>
    /// <response code="400">Validation failed (past date, duration outside 15–240 minutes, bad meeting link).</response>
    /// <response code="404">The application does not exist or is not on one of your jobs.</response>
    /// <response code="409">The application is closed, or the slot overlaps another interview.</response>
    [HttpPost]
    [Authorize(Roles = RoleNames.Recruiter)]
    [ProducesResponseType(typeof(InterviewCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InterviewCreatedDto>> Create(CreateInterviewDto request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.InterviewId }, result);
    }

    /// <summary>Reschedules or amends an interview. A time change re-sends the invitation.</summary>
    /// <response code="200">Interview updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Interview not found or not yours.</response>
    /// <response code="409">The new slot overlaps another interview, or the interview is cancelled.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.Recruiter)]
    [ProducesResponseType(typeof(InterviewResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InterviewResponseDto>> Update(Guid id, UpdateInterviewDto request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(CurrentUserId, id, request, cancellationToken));

    /// <summary>
    /// Cancels an interview and emails a calendar cancellation. The record is retained (status
    /// becomes Cancelled) rather than deleted, so the audit trail and any feedback survive.
    /// </summary>
    /// <response code="204">Interview cancelled.</response>
    /// <response code="404">Interview not found or not yours.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.Recruiter)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _service.CancelAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lists interviews visible to the caller: a recruiter sees interviews on their own jobs, a
    /// candidate sees their own.
    /// </summary>
    /// <param name="upcomingOnly">When true, returns only future, still-scheduled interviews.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InterviewResponseDto>>> GetAll(
        [FromQuery] bool upcomingOnly, CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(CurrentUserId, upcomingOnly, cancellationToken));

    /// <summary>Returns a single interview, including its calendar links.</summary>
    /// <response code="404">Interview not found or not visible to you.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InterviewResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InterviewResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(CurrentUserId, id, cancellationToken));
}
