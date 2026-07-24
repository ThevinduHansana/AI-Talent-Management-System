using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.DTOs.Candidates;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>Candidate self-service: profile, skills, education, experience and certificates.</summary>
[Authorize(Roles = RoleNames.Candidate)]
[Route("api/candidate")]
public class CandidateController : ApiControllerBase
{
    private readonly ICandidateService _candidateService;

    public CandidateController(ICandidateService candidateService) => _candidateService = candidateService;

    /// <summary>Returns the authenticated candidate's full profile.</summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CandidateProfileDto>> GetProfile(CancellationToken cancellationToken)
        => Ok(await _candidateService.GetProfileAsync(CurrentUserId, cancellationToken));

    /// <summary>Updates the candidate's profile details.</summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CandidateProfileDto>> UpdateProfile(UpdateCandidateProfileRequest request, CancellationToken cancellationToken)
        => Ok(await _candidateService.UpdateProfileAsync(CurrentUserId, request, cancellationToken));

    // ----- Skills -----

    [HttpPost("skills")]
    [ProducesResponseType(typeof(CandidateSkillDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CandidateSkillDto>> AddSkill(AddCandidateSkillRequest request, CancellationToken cancellationToken)
    {
        var result = await _candidateService.AddSkillAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("skills/{candidateSkillId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveSkill(Guid candidateSkillId, CancellationToken cancellationToken)
    {
        await _candidateService.RemoveSkillAsync(CurrentUserId, candidateSkillId, cancellationToken);
        return NoContent();
    }

    // ----- Education -----

    [HttpPost("education")]
    [ProducesResponseType(typeof(EducationDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<EducationDto>> AddEducation(SaveEducationRequest request, CancellationToken cancellationToken)
    {
        var result = await _candidateService.AddEducationAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("education/{educationId:guid}")]
    [ProducesResponseType(typeof(EducationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EducationDto>> UpdateEducation(Guid educationId, SaveEducationRequest request, CancellationToken cancellationToken)
        => Ok(await _candidateService.UpdateEducationAsync(CurrentUserId, educationId, request, cancellationToken));

    [HttpDelete("education/{educationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveEducation(Guid educationId, CancellationToken cancellationToken)
    {
        await _candidateService.RemoveEducationAsync(CurrentUserId, educationId, cancellationToken);
        return NoContent();
    }

    // ----- Experience -----

    [HttpPost("experience")]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ExperienceDto>> AddExperience(SaveExperienceRequest request, CancellationToken cancellationToken)
    {
        var result = await _candidateService.AddExperienceAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("experience/{experienceId:guid}")]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExperienceDto>> UpdateExperience(Guid experienceId, SaveExperienceRequest request, CancellationToken cancellationToken)
        => Ok(await _candidateService.UpdateExperienceAsync(CurrentUserId, experienceId, request, cancellationToken));

    [HttpDelete("experience/{experienceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveExperience(Guid experienceId, CancellationToken cancellationToken)
    {
        await _candidateService.RemoveExperienceAsync(CurrentUserId, experienceId, cancellationToken);
        return NoContent();
    }

    // ----- Certificates -----

    [HttpPost("certificates")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CertificateDto>> AddCertificate(SaveCertificateRequest request, CancellationToken cancellationToken)
    {
        var result = await _candidateService.AddCertificateAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("certificates/{certificateId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveCertificate(Guid certificateId, CancellationToken cancellationToken)
    {
        await _candidateService.RemoveCertificateAsync(CurrentUserId, certificateId, cancellationToken);
        return NoContent();
    }
}
