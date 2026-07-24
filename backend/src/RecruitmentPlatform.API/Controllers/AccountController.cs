using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.DTOs.Account;
using RecruitmentPlatform.Application.Interfaces.Services;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>
/// Self-service data-privacy endpoints for the signed-in user: export a copy of their personal
/// data (right to access) and erase their account (right to erasure).
/// </summary>
[Authorize]
[Route("api/account")]
public class AccountController : ApiControllerBase
{
    private readonly IAccountService _account;

    public AccountController(IAccountService account) => _account = account;

    /// <summary>
    /// Downloads a JSON file containing all personal data the platform holds for the current user —
    /// profile, applications, interviews, saved jobs, resume metadata, notifications and messages.
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(PersonalDataExport), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportData(CancellationToken cancellationToken)
    {
        var data = await _account.ExportPersonalDataAsync(CurrentUserId, cancellationToken);

        var json = JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        // Delivered as an attachment so the browser saves it rather than rendering it.
        var fileName = $"getcareers-data-export-{DateTime.UtcNow:yyyyMMdd}.json";
        return File(json, "application/json", fileName);
    }

    /// <summary>
    /// Permanently erases the current user's account: deletes their resume documents, anonymises
    /// their identity and contact details, and revokes all sessions. This cannot be undone.
    /// </summary>
    /// <response code="200">Account erased. Any stored tokens should be discarded by the client.</response>
    [HttpDelete]
    [ProducesResponseType(typeof(AccountDeletionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountDeletionResult>> DeleteAccount(CancellationToken cancellationToken)
        => Ok(await _account.DeleteAccountAsync(CurrentUserId, cancellationToken));
}
