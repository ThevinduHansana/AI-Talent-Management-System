namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>
/// A calendar event rendered into provider links and an .ics file. All times are UTC — callers
/// must convert before constructing this.
/// </summary>
/// <param name="Uid">Stable RFC 5545 UID, so re-sends update rather than duplicate the entry.</param>
/// <param name="Sequence">RFC 5545 SEQUENCE; must increase for clients to accept an update.</param>
public record CalendarInvite(
    string Uid,
    int Sequence,
    string Title,
    string Description,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Location,
    string? Url,
    string OrganizerName,
    string OrganizerEmail,
    string AttendeeName,
    string AttendeeEmail,
    bool IsCancelled = false);

/// <summary>
/// Builds "add to calendar" deep links. These are plain URLs the recipient clicks — no OAuth and
/// no provider API calls, so nothing needs authorising and the links never expire.
/// </summary>
public interface ICalendarLinkService
{
    /// <summary>Google Calendar pre-filled event URL.</summary>
    string BuildGoogleCalendarUrl(CalendarInvite invite);

    /// <summary>Outlook Web (outlook.live.com) pre-filled event URL.</summary>
    string BuildOutlookCalendarUrl(CalendarInvite invite);
}

/// <summary>Generates RFC 5545 (iCalendar) files for interview invitations.</summary>
public interface IIcsGeneratorService
{
    /// <summary>Returns the .ics document as UTF-8 bytes, ready to attach to an email.</summary>
    byte[] Generate(CalendarInvite invite);
}

/// <summary>Provider deep links for a single interview.</summary>
public record CalendarLinks(string Google, string Outlook)
{
    public static CalendarLinks Empty { get; } = new(string.Empty, string.Empty);
}

/// <summary>
/// Sends interview calendar invitations. Every scheduling path delegates here so the invitation
/// is identical regardless of which endpoint created the interview.
/// </summary>
public interface IInterviewInvitationService
{
    /// <summary>Builds the provider links for an already-loaded interview, sending nothing.</summary>
    CalendarLinks BuildLinks(Domain.Entities.InterviewSchedule interview);

    /// <summary>Emails the invitation (with .ics attached) and returns the provider links.</summary>
    Task<CalendarLinks> SendInvitationAsync(Guid interviewId, bool isReschedule, CancellationToken cancellationToken = default);

    /// <summary>Emails a calendar cancellation, removing the event from the attendee's calendar.</summary>
    Task SendCancellationAsync(Guid interviewId, CancellationToken cancellationToken = default);
}
