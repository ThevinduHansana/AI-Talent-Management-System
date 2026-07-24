namespace RecruitmentPlatform.Application.DTOs.Account;

/// <summary>
/// A machine-readable snapshot of everything the platform holds about a user, produced for the
/// "right to access" (data portability) endpoint. Serialised to JSON and returned as a download.
/// </summary>
public record PersonalDataExport(
    DateTime GeneratedAtUtc,
    AccountInfo Account,
    IReadOnlyList<string> Roles,
    CandidateProfileExport? CandidateProfile,
    IReadOnlyList<ApplicationExport> Applications,
    IReadOnlyList<InterviewExport> Interviews,
    IReadOnlyList<SavedJobExport> SavedJobs,
    IReadOnlyList<ResumeExport> Resumes,
    IReadOnlyList<NotificationExport> Notifications,
    IReadOnlyList<MessageExport> Messages);

public record AccountInfo(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsEmailConfirmed,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record CandidateProfileExport(
    string? Headline,
    string? Summary,
    string? Location,
    string? CurrentPosition,
    int YearsOfExperience,
    string? LinkedInUrl,
    string? PortfolioUrl,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Education,
    IReadOnlyList<string> Experience);

public record ApplicationExport(
    Guid Id,
    string JobTitle,
    string? CompanyName,
    string Status,
    DateTime AppliedAt,
    string? CoverLetter);

public record InterviewExport(
    Guid Id,
    string Title,
    DateTime ScheduledAtUtc,
    int DurationMinutes,
    string Status,
    string? Location,
    string? MeetingLink);

public record SavedJobExport(Guid JobId, string JobTitle, DateTime SavedAt);

public record ResumeExport(Guid Id, string FileName, long FileSize, bool IsPrimary, DateTime UploadedAt);

public record NotificationExport(string Title, string Message, string Type, bool IsRead, DateTime CreatedAt);

public record MessageExport(string Direction, string Content, bool IsRead, DateTime SentAt);

/// <summary>Result of erasing an account.</summary>
public record AccountDeletionResult(string Message, int ResumesDeleted);
