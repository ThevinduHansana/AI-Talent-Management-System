using RecruitmentPlatform.Application.DTOs.Account;

namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>
/// Self-service data-privacy operations for the authenticated user: exporting their personal data
/// (right to access) and erasing their account (right to erasure).
/// </summary>
public interface IAccountService
{
    /// <summary>Gathers all personal data held for the user into a single exportable object.</summary>
    Task<PersonalDataExport> ExportPersonalDataAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Erases the user's account: deletes their resume documents, anonymises their identity and
    /// contact details, revokes their sessions and disables login. Records that reference the user
    /// (e.g. applications a recruiter reviewed) are retained but de-identified, preserving
    /// referential integrity while removing the personal data.
    /// </summary>
    Task<AccountDeletionResult> DeleteAccountAsync(Guid userId, CancellationToken cancellationToken = default);
}
