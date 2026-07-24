using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Common.Email;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Builds and delivers interview calendar invitations. Extracted so that every scheduling path
/// issues the same invitation — the recruiter pipeline flow and the calendar-aware endpoint both
/// call this rather than each rolling their own, which previously meant one of them sent no
/// calendar invite at all.
/// </summary>
public class InterviewInvitationService : IInterviewInvitationService
{
    private readonly IUnitOfWork _uow;
    private readonly ICalendarLinkService _calendarLinks;
    private readonly IIcsGeneratorService _ics;
    private readonly IEmailService _email;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<InterviewInvitationService> _logger;

    public InterviewInvitationService(
        IUnitOfWork uow,
        ICalendarLinkService calendarLinks,
        IIcsGeneratorService ics,
        IEmailService email,
        IOptions<EmailSettings> emailSettings,
        ILogger<InterviewInvitationService> logger)
    {
        _uow = uow;
        _calendarLinks = calendarLinks;
        _ics = ics;
        _email = email;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public CalendarLinks BuildLinks(InterviewSchedule interview)
    {
        var invite = BuildInvite(interview);
        return new CalendarLinks(
            _calendarLinks.BuildGoogleCalendarUrl(invite),
            _calendarLinks.BuildOutlookCalendarUrl(invite));
    }

    public async Task<CalendarLinks> SendInvitationAsync(Guid interviewId, bool isReschedule, CancellationToken cancellationToken = default)
    {
        var interview = await _uow.Interviews.GetWithDetailsAsync(interviewId, cancellationToken);
        if (interview is null)
        {
            _logger.LogWarning("Cannot send an invitation: interview {InterviewId} was not found.", interviewId);
            return CalendarLinks.Empty;
        }

        var invite = BuildInvite(interview);
        var google = _calendarLinks.BuildGoogleCalendarUrl(invite);
        var outlook = _calendarLinks.BuildOutlookCalendarUrl(invite);

        var job = interview.Application.Job;
        var candidateUser = interview.Application.Candidate.User;

        try
        {
            var icsBytes = _ics.Generate(invite);

            var html = EmailTemplates.InterviewInvitation(new EmailTemplates.InterviewInvitationModel(
                CandidateName: candidateUser.FirstName,
                CompanyName: job.Organization?.Name ?? "GetCareers",
                JobTitle: job.Title,
                StartUtc: interview.ScheduledAt,
                DurationMinutes: interview.DurationMinutes,
                Location: interview.Location,
                MeetingLink: interview.MeetingLink,
                Notes: interview.Notes,
                GoogleCalendarUrl: google,
                OutlookCalendarUrl: outlook,
                IsReschedule: isReschedule));

            await _email.SendAsync(
                candidateUser.Email,
                $"Interview Invitation – {job.Title}",
                html,
                new[] { new EmailAttachment("interview.ics", "text/calendar; method=REQUEST; charset=utf-8", icsBytes) },
                cancellationToken);

            _logger.LogInformation("Invitation email sent for interview {InterviewId} to {Email}.",
                interviewId, candidateUser.Email);
        }
        catch (Exception ex)
        {
            // The interview is already committed; a mail failure must not fail the request.
            _logger.LogError(ex, "Failed to send the invitation email for interview {InterviewId}.", interviewId);
        }

        return new CalendarLinks(google, outlook);
    }

    public async Task SendCancellationAsync(Guid interviewId, CancellationToken cancellationToken = default)
    {
        var interview = await _uow.Interviews.GetWithDetailsAsync(interviewId, cancellationToken);
        if (interview is null)
        {
            return;
        }

        var invite = BuildInvite(interview) with { IsCancelled = true };
        var job = interview.Application.Job;
        var candidateUser = interview.Application.Candidate.User;

        try
        {
            var icsBytes = _ics.Generate(invite);

            var html = EmailTemplates.Build(
                "Your interview has been cancelled",
                $"Your interview for \"{job.Title}\" scheduled for {interview.ScheduledAt:f} UTC has been cancelled. "
                + "The attached calendar file will remove it from your calendar.",
                footnote: "If you believe this is a mistake, reply to this email to contact the recruiter.");

            await _email.SendAsync(
                candidateUser.Email,
                $"Interview Cancelled – {job.Title}",
                html,
                new[] { new EmailAttachment("interview.ics", "text/calendar; method=CANCEL; charset=utf-8", icsBytes) },
                cancellationToken);

            _logger.LogInformation("Cancellation email sent for interview {InterviewId}.", interviewId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send the cancellation email for interview {InterviewId}.", interviewId);
        }
    }

    private CalendarInvite BuildInvite(InterviewSchedule interview)
    {
        var application = interview.Application;
        var job = application.Job;
        var company = job.Organization?.Name ?? "GetCareers";
        var candidateUser = application.Candidate.User;

        var description = $"Interview for {job.Title} at {company}."
                          + (string.IsNullOrWhiteSpace(interview.Notes) ? string.Empty : $"\n\nNotes: {interview.Notes}");

        return new CalendarInvite(
            Uid: interview.CalendarUid ?? $"{interview.Id:N}@getcareers",
            Sequence: interview.CalendarSequence,
            Title: string.IsNullOrWhiteSpace(interview.Title) ? $"Interview — {job.Title}" : interview.Title,
            Description: description,
            StartUtc: interview.ScheduledAt,
            EndUtc: interview.ScheduledAt.AddMinutes(interview.DurationMinutes),
            Location: string.IsNullOrWhiteSpace(interview.MeetingLink) ? interview.Location : interview.MeetingLink,
            Url: interview.MeetingLink,
            OrganizerName: company,
            OrganizerEmail: string.IsNullOrWhiteSpace(_emailSettings.ResolvedFromAddress)
                ? "no-reply@getcareers.local"
                : _emailSettings.ResolvedFromAddress,
            AttendeeName: candidateUser.FullName,
            AttendeeEmail: candidateUser.Email);
    }
}
