using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Interfaces.Infrastructure;

/// <summary>Hashes and verifies passwords. Abstracted so the algorithm can be swapped.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

/// <summary>Issues JWT access tokens and cryptographically random refresh tokens.</summary>
public interface ITokenService
{
    (string token, DateTime expiresAt) GenerateAccessToken(User user, IEnumerable<string> roles);

    string GenerateRefreshToken();
}

/// <summary>Result of storing a file via <see cref="IFileStorageService"/>.</summary>
public record StoredFile(string StoredFileName, string Path, string ContentType, long Size);

/// <summary>
/// Abstraction over file storage. Local and cloud implementations are interchangeable so
/// business logic never depends on where files live.
/// </summary>
public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType, string category, CancellationToken cancellationToken = default);

    Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>A file attached to an outgoing email (e.g. a calendar invitation).</summary>
/// <param name="FileName">Name shown to the recipient, e.g. "interview.ics".</param>
/// <param name="ContentType">MIME type, e.g. "text/calendar; method=REQUEST".</param>
/// <param name="Content">Raw bytes of the attachment.</param>
public record EmailAttachment(string FileName, string ContentType, byte[] Content);

/// <summary>Sends transactional email. Swappable (SMTP, SendGrid, etc.).</summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>Sends a message with attachments, used for .ics calendar invitations.</summary>
    Task SendAsync(string to, string subject, string htmlBody, IReadOnlyCollection<EmailAttachment> attachments,
        CancellationToken cancellationToken = default);
}

/// <summary>Sends SMS notifications. Swappable (Twilio, etc.).</summary>
public interface ISmsService
{
    Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default);
}

/// <summary>Represents an external calendar event to create/update.</summary>
public record CalendarEvent(string Title, string? Description, DateTime StartUtc, DateTime EndUtc, string? Location, IEnumerable<string> AttendeeEmails);

/// <summary>Abstraction over calendar providers (Google, Outlook).</summary>
public interface ICalendarService
{
    Task<string> CreateEventAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);

    Task CancelEventAsync(string eventId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Extracts plain text from an uploaded document (PDF / Word). Abstracted so the extraction
/// engine can be swapped or replaced by an external parsing/AI service.
/// </summary>
public interface ITextExtractor
{
    /// <summary>Returns extracted text, or an empty string when the format is unsupported.</summary>
    Task<string> ExtractTextAsync(Stream content, string contentType, string fileName, CancellationToken cancellationToken = default);
}
