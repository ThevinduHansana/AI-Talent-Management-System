using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.DTOs.Candidates;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>
/// Multipart form payload for a resume upload. Wrapping the file and flag in a single
/// [FromForm] model keeps Swashbuckle able to describe the endpoint (it cannot when an IFormFile
/// and a separate [FromForm] scalar are declared as sibling parameters).
/// </summary>
public class ResumeUploadRequest
{
    public IFormFile? File { get; set; }

    public bool MakePrimary { get; set; }
}

/// <summary>Candidate resume document management (upload, download, delete, set primary).</summary>
[Authorize(Roles = RoleNames.Candidate)]
public class ResumesController : ApiControllerBase
{
    private readonly IDocumentService _documentService;

    public ResumesController(IDocumentService documentService) => _documentService = documentService;

    /// <summary>Uploads a PDF or Word resume (max 5 MB).</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(ResumeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResumeDto>> Upload([FromForm] ResumeUploadRequest request, CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { title = "A non-empty file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _documentService.UploadResumeAsync(
            CurrentUserId, stream, file.FileName, file.ContentType, file.Length, request.MakePrimary, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Downloads a resume document owned by the authenticated candidate.</summary>
    [HttpGet("{resumeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid resumeId, CancellationToken cancellationToken)
    {
        var (content, contentType, fileName) = await _documentService.DownloadResumeAsync(CurrentUserId, resumeId, cancellationToken);
        return File(content, contentType, fileName);
    }

    /// <summary>Marks a resume as the candidate's primary resume.</summary>
    [HttpPost("{resumeId:guid}/primary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPrimary(Guid resumeId, CancellationToken cancellationToken)
    {
        await _documentService.SetPrimaryResumeAsync(CurrentUserId, resumeId, cancellationToken);
        return NoContent();
    }

    /// <summary>Deletes a resume document.</summary>
    [HttpDelete("{resumeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid resumeId, CancellationToken cancellationToken)
    {
        await _documentService.DeleteResumeAsync(CurrentUserId, resumeId, cancellationToken);
        return NoContent();
    }
}
