using Microsoft.Extensions.Logging;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;

namespace RecruitmentPlatform.Infrastructure.Services;

/// <summary>
/// Development email channel that logs messages instead of sending them. Replace the
/// registration with an SMTP/SendGrid implementation for production without touching callers.
/// </summary>
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => SendAsync(to, subject, htmlBody, Array.Empty<EmailAttachment>(), cancellationToken);

    public Task SendAsync(string to, string subject, string htmlBody,
        IReadOnlyCollection<EmailAttachment> attachments, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("EMAIL -> {To} | {Subject} | {Count} attachment(s)", to, subject, attachments.Count);
        return Task.CompletedTask;
    }
}

/// <summary>Development SMS channel that logs instead of sending.</summary>
public class LoggingSmsService : ISmsService
{
    private readonly ILogger<LoggingSmsService> _logger;

    public LoggingSmsService(ILogger<LoggingSmsService> logger) => _logger = logger;

    public Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SMS -> {Phone} | {Message}", toPhoneNumber, message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Development calendar provider that records events in memory/logs and returns a synthetic
/// event id. Swap for Google/Outlook implementations behind the same interface.
/// </summary>
public class LoggingCalendarService : ICalendarService
{
    private readonly ILogger<LoggingCalendarService> _logger;

    public LoggingCalendarService(ILogger<LoggingCalendarService> logger) => _logger = logger;

    public Task<string> CreateEventAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        var eventId = $"local-{Guid.NewGuid():N}";
        _logger.LogInformation("CALENDAR create {EventId}: {Title} @ {Start:u}", eventId, calendarEvent.Title, calendarEvent.StartUtc);
        return Task.FromResult(eventId);
    }

    public Task CancelEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CALENDAR cancel {EventId}", eventId);
        return Task.CompletedTask;
    }
}
