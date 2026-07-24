using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.HiringManager;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>
/// Hiring-manager endpoints: review queue, candidate evaluations, interview feedback and the
/// final hiring decision. All operations are scoped to the manager's organization.
/// </summary>
[Authorize(Roles = RoleNames.HiringManager)]
[Route("api/hiring-manager")]
public class HiringManagerController : ApiControllerBase
{
    private readonly IHiringManagerService _service;

    public HiringManagerController(IHiringManagerService service) => _service = service;

    /// <summary>Returns headline metrics for the hiring-manager dashboard.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(HiringManagerDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HiringManagerDashboardDto>> Dashboard(CancellationToken cancellationToken)
        => Ok(await _service.GetDashboardAsync(CurrentUserId, cancellationToken));

    /// <summary>Lists candidates awaiting review within the manager's organization.</summary>
    [HttpGet("review-queue")]
    [ProducesResponseType(typeof(PagedResult<ReviewCandidateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReviewCandidateDto>>> ReviewQueue([FromQuery] ReviewQueueQuery query, CancellationToken cancellationToken)
        => Ok(await _service.GetReviewQueueAsync(CurrentUserId, query, cancellationToken));

    /// <summary>Returns the full review detail for a candidate's application.</summary>
    [HttpGet("candidates/{applicationId:guid}")]
    [ProducesResponseType(typeof(CandidateReviewDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateReviewDetailDto>> CandidateDetail(Guid applicationId, CancellationToken cancellationToken)
        => Ok(await _service.GetCandidateDetailAsync(CurrentUserId, applicationId, cancellationToken));

    /// <summary>Downloads a candidate's resume for a reviewable application within the manager's organization.</summary>
    [HttpGet("candidates/{applicationId:guid}/resumes/{resumeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadResume(Guid applicationId, Guid resumeId, CancellationToken cancellationToken)
    {
        var (content, contentType, fileName) = await _service.DownloadCandidateResumeAsync(CurrentUserId, applicationId, resumeId, cancellationToken);
        return File(content, contentType, fileName);
    }

    /// <summary>Creates or updates the manager's evaluation for an application.</summary>
    [HttpPost("evaluations")]
    [ProducesResponseType(typeof(EvaluationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EvaluationDto>> SubmitEvaluation(SubmitEvaluationRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SubmitEvaluationAsync(CurrentUserId, request, cancellationToken));

    /// <summary>Creates or updates interview feedback for a scheduled interview.</summary>
    [HttpPost("interview-feedback")]
    [ProducesResponseType(typeof(InterviewFeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InterviewFeedbackDto>> SubmitFeedback(SubmitInterviewFeedbackRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SubmitInterviewFeedbackAsync(CurrentUserId, request, cancellationToken));

    /// <summary>Approves hiring for an application (marks the candidate as hired).</summary>
    [HttpPost("candidates/{applicationId:guid}/approve")]
    [ProducesResponseType(typeof(EvaluationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EvaluationDto>> Approve(Guid applicationId, HiringDecisionRequest request, CancellationToken cancellationToken)
        => Ok(await _service.ApproveHiringAsync(CurrentUserId, applicationId, request, cancellationToken));

    /// <summary>Rejects hiring for an application.</summary>
    [HttpPost("candidates/{applicationId:guid}/reject")]
    [ProducesResponseType(typeof(EvaluationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EvaluationDto>> Reject(Guid applicationId, HiringDecisionRequest request, CancellationToken cancellationToken)
        => Ok(await _service.RejectHiringAsync(CurrentUserId, applicationId, request, cancellationToken));
}
