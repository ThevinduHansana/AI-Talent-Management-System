using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.DTOs.Ai;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>
/// AI-assisted candidate features: resume parsing/skill extraction, job recommendations and
/// automated application feedback.
/// </summary>
[Authorize(Roles = RoleNames.Candidate)]
[Route("api/ai")]
public class AiController : ApiControllerBase
{
    private readonly IAiService _service;

    public AiController(IAiService service) => _service = service;

    /// <summary>
    /// Parses a resume, extracts skills and returns an analysis. Pass <c>autoAddSkills=true</c> to
    /// add newly detected skills to the candidate's profile.
    /// </summary>
    [HttpPost("resumes/{resumeId:guid}/analyze")]
    [ProducesResponseType(typeof(ResumeAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeAnalysisResultDto>> AnalyzeResume(Guid resumeId, [FromQuery] bool autoAddSkills = false, CancellationToken cancellationToken = default)
        => Ok(await _service.AnalyzeResumeAsync(CurrentUserId, resumeId, autoAddSkills, cancellationToken));

    /// <summary>Returns AI-ranked job recommendations for the authenticated candidate.</summary>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(IReadOnlyList<JobRecommendationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobRecommendationDto>>> Recommendations([FromQuery] int count = 5, CancellationToken cancellationToken = default)
        => Ok(await _service.RecommendJobsAsync(CurrentUserId, count, cancellationToken));

    /// <summary>
    /// Generates automated feedback on one of the authenticated candidate's own applications:
    /// fit summary, strengths, gaps and concrete next steps.
    /// </summary>
    [HttpGet("applications/{applicationId:guid}/feedback")]
    [ProducesResponseType(typeof(ApplicationFeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationFeedbackDto>> ApplicationFeedback(Guid applicationId, CancellationToken cancellationToken = default)
        => Ok(await _service.GenerateApplicationFeedbackAsync(CurrentUserId, applicationId, cancellationToken));
}
