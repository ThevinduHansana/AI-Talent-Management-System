using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Common.Email;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Interviews;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Application.Validators.Interviews;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Schedules interviews and issues calendar invitations.
/// <para>
/// Calendar integration is link-based (Google/Outlook deep links plus an .ics attachment) rather
/// than API-based, so no OAuth consent or provider credentials are required and reminders are
/// handled by the candidate's own calendar once the event is added.
/// </para>
/// </summary>
public class InterviewSchedulingService : IInterviewSchedulingService
{
    private readonly IUnitOfWork _uow;
    private readonly IInterviewInvitationService _invitations;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;
    private readonly ILogger<InterviewSchedulingService> _logger;

    public InterviewSchedulingService(
        IUnitOfWork uow,
        IInterviewInvitationService invitations,
        INotificationService notifications,
        IAuditService audit,
        ILogger<InterviewSchedulingService> logger)
    {
        _uow = uow;
        _invitations = invitations;
        _notifications = notifications;
        _audit = audit;
        _logger = logger;
    }

    public async Task<InterviewCreatedDto> CreateAsync(Guid userId, CreateInterviewDto request, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        var startUtc = InterviewRules.ToUtc(request.InterviewDate);
        var endUtc = startUtc.AddMinutes(request.DurationMinutes);

        // The application is the single source of truth for candidate + job + owning recruiter,
        // and scoping by RecruiterId here also enforces that this recruiter owns the job.
        var application = await _uow.Applications.Query().AsTracking()
            .Include(a => a.Job).ThenInclude(j => j.Organization)
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.Job.RecruiterId == recruiter.Id, cancellationToken)
            ?? throw new NotFoundException("Application", request.ApplicationId);

        if (application.Status is ApplicationStatus.Rejected or ApplicationStatus.Withdrawn or ApplicationStatus.Hired)
        {
            throw new ConflictException($"Cannot schedule an interview for an application in '{application.Status}' state.");
        }

        if (await _uow.Interviews.HasCandidateOverlapAsync(application.CandidateId, startUtc, endUtc, null, cancellationToken))
        {
            throw new ConflictException("This candidate already has an interview scheduled that overlaps this time slot.");
        }

        var interview = new InterviewSchedule
        {
            ApplicationId = application.Id,
            ScheduledByUserId = userId,
            InterviewerUserId = request.InterviewerUserId,
            Title = request.Title.Trim(),
            ScheduledAt = startUtc,
            DurationMinutes = request.DurationMinutes,
            Mode = request.Mode,
            Location = request.Location,
            MeetingLink = request.MeetingLink,
            Status = InterviewStatus.Scheduled,
            Notes = request.Notes,
            // Stable UID so a later reschedule updates this entry rather than creating a second one.
            CalendarUid = $"{Guid.NewGuid():N}@getcareers",
            CalendarSequence = 0,
        };

        await _uow.Interviews.AddAsync(interview, cancellationToken);

        // Advance the pipeline, mirroring the existing recruiter scheduling flow.
        if (application.Status is ApplicationStatus.Applied or ApplicationStatus.UnderReview or ApplicationStatus.Shortlisted)
        {
            application.Status = ApplicationStatus.InterviewScheduled;
            application.StatusChangedAt = DateTime.UtcNow;
        }

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Interview created {InterviewId} for application {ApplicationId} at {StartUtc:u} ({Duration}m).",
            interview.Id, application.Id, startUtc, interview.DurationMinutes);

        await _audit.LogAsync("InterviewScheduled", nameof(InterviewSchedule), interview.Id.ToString(),
            $"Interview scheduled for application {application.Id} at {startUtc:u}.", userId, cancellationToken: cancellationToken);

        var links = await _invitations.SendInvitationAsync(interview.Id, isReschedule: false, cancellationToken);

        await NotifyCandidateAsync(application, startUtc, "Interview scheduled",
            $"An interview for \"{application.Job.Title}\" is scheduled for {startUtc:f} UTC.", cancellationToken);

        return new InterviewCreatedDto(
            interview.Id,
            links.Google,
            links.Outlook,
            interview.Status,
            "Interview scheduled successfully and invitation email sent.");
    }

    public async Task<InterviewResponseDto> UpdateAsync(Guid userId, Guid interviewId, UpdateInterviewDto request, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        var startUtc = InterviewRules.ToUtc(request.InterviewDate);
        var endUtc = startUtc.AddMinutes(request.DurationMinutes);

        var interview = await LoadOwnedAsync(interviewId, recruiter.Id, cancellationToken);

        if (interview.Status == InterviewStatus.Cancelled)
        {
            throw new ConflictException("A cancelled interview cannot be updated. Schedule a new one instead.");
        }

        if (await _uow.Interviews.HasCandidateOverlapAsync(interview.Application.CandidateId, startUtc, endUtc, interviewId, cancellationToken))
        {
            throw new ConflictException("This candidate already has an interview scheduled that overlaps this time slot.");
        }

        var timeChanged = interview.ScheduledAt != startUtc || interview.DurationMinutes != request.DurationMinutes;

        interview.Title = request.Title.Trim();
        interview.ScheduledAt = startUtc;
        interview.DurationMinutes = request.DurationMinutes;
        interview.Mode = request.Mode;
        interview.Location = request.Location;
        interview.MeetingLink = request.MeetingLink;
        interview.InterviewerUserId = request.InterviewerUserId;
        interview.Notes = request.Notes;

        if (request.Status is { } status)
        {
            interview.Status = status;
        }

        if (timeChanged)
        {
            // A moved interview must re-arm both reminders, otherwise a reminder already sent for
            // the old slot would suppress the one for the new slot.
            interview.FirstReminderSentAt = null;
            interview.SecondReminderSentAt = null;
            // RFC 5545: calendar clients ignore an update unless SEQUENCE increases.
            interview.CalendarSequence += 1;
        }

        interview.CalendarUid ??= $"{Guid.NewGuid():N}@getcareers";

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Interview updated {InterviewId} (time changed: {TimeChanged}).", interview.Id, timeChanged);

        await _audit.LogAsync("InterviewUpdated", nameof(InterviewSchedule), interview.Id.ToString(),
            $"Interview {interview.Id} updated (time changed: {timeChanged}).", userId, cancellationToken: cancellationToken);

        var links = _invitations.BuildLinks(interview);

        if (timeChanged && interview.Status == InterviewStatus.Scheduled)
        {
            links = await _invitations.SendInvitationAsync(interview.Id, isReschedule: true, cancellationToken);

            await NotifyCandidateAsync(interview.Application, startUtc, "Interview rescheduled",
                $"Your interview for \"{interview.Application.Job.Title}\" has moved to {startUtc:f} UTC.", cancellationToken);
        }

        return ToResponse(interview, links.Google, links.Outlook);
    }

    public async Task CancelAsync(Guid userId, Guid interviewId, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        var interview = await LoadOwnedAsync(interviewId, recruiter.Id, cancellationToken);

        if (interview.Status == InterviewStatus.Cancelled)
        {
            return; // Idempotent: cancelling twice is not an error.
        }

        interview.Status = InterviewStatus.Cancelled;
        interview.CalendarSequence += 1;
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Interview cancelled {InterviewId}.", interview.Id);

        await _audit.LogAsync("InterviewCancelled", nameof(InterviewSchedule), interview.Id.ToString(),
            $"Interview {interview.Id} cancelled.", userId, cancellationToken: cancellationToken);

        // A CANCEL-method .ics removes the event from the candidate's calendar automatically.
        await _invitations.SendCancellationAsync(interview.Id, cancellationToken);

        await NotifyCandidateAsync(interview.Application, interview.ScheduledAt, "Interview cancelled",
            $"Your interview for \"{interview.Application.Job.Title}\" on {interview.ScheduledAt:f} UTC has been cancelled.",
            cancellationToken);
    }

    public async Task<IReadOnlyList<InterviewResponseDto>> GetAllAsync(Guid userId, bool upcomingOnly = false, CancellationToken cancellationToken = default)
    {
        var query = await BuildVisibilityScopedQueryAsync(userId, cancellationToken);

        if (upcomingOnly)
        {
            query = query.Where(i => i.Status == InterviewStatus.Scheduled && i.ScheduledAt >= DateTime.UtcNow);
        }

        var interviews = await query
            .Include(i => i.Application).ThenInclude(a => a.Job).ThenInclude(j => j.Organization)
            .Include(i => i.Application).ThenInclude(a => a.Candidate).ThenInclude(c => c.User)
            .OrderBy(i => i.ScheduledAt)
            .ToListAsync(cancellationToken);

        return interviews.Select(BuildResponse).ToList();
    }

    public async Task<InterviewResponseDto> GetByIdAsync(Guid userId, Guid interviewId, CancellationToken cancellationToken = default)
    {
        var query = await BuildVisibilityScopedQueryAsync(userId, cancellationToken);

        var interview = await query
            .Include(i => i.Application).ThenInclude(a => a.Job).ThenInclude(j => j.Organization)
            .Include(i => i.Application).ThenInclude(a => a.Candidate).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken)
            ?? throw new NotFoundException("Interview", interviewId);

        return BuildResponse(interview);
    }

    // ----- helpers -----

    /// <summary>
    /// Restricts the query to what the caller may see. Recruiters get interviews on their own
    /// jobs; candidates get their own. Anyone else sees nothing rather than everything — failing
    /// closed matters here because interviews carry personal data.
    /// </summary>
    private async Task<IQueryable<InterviewSchedule>> BuildVisibilityScopedQueryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var all = _uow.Interviews.Query();

        var recruiter = await _uow.Repository<Recruiter>()
            .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);
        if (recruiter is not null)
        {
            return all.Where(i => i.Application.Job.RecruiterId == recruiter.Id);
        }

        var candidate = await _uow.Candidates.GetByUserIdAsync(userId, cancellationToken);
        if (candidate is not null)
        {
            return all.Where(i => i.Application.CandidateId == candidate.Id);
        }

        // Interviewers who are neither: show the ones they are assigned to.
        return all.Where(i => i.InterviewerUserId == userId);
    }

    private async Task<InterviewSchedule> LoadOwnedAsync(Guid interviewId, Guid recruiterId, CancellationToken cancellationToken)
        => await _uow.Interviews.Query().AsTracking()
               .Include(i => i.Application).ThenInclude(a => a.Job).ThenInclude(j => j.Organization)
               .Include(i => i.Application).ThenInclude(a => a.Candidate).ThenInclude(c => c.User)
               .FirstOrDefaultAsync(i => i.Id == interviewId && i.Application.Job.RecruiterId == recruiterId, cancellationToken)
           ?? throw new NotFoundException("Interview", interviewId);

    private async Task NotifyCandidateAsync(JobApplication application, DateTime whenUtc, string title, string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _notifications.NotifyAsync(
                application.Candidate.UserId, title, message, NotificationType.Interview,
                $"/candidate/applications/{application.Id}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to raise the in-app interview notification for application {ApplicationId}.", application.Id);
        }
    }

    private InterviewResponseDto BuildResponse(InterviewSchedule interview)
    {
        var links = _invitations.BuildLinks(interview);
        return ToResponse(interview, links.Google, links.Outlook);
    }

    private static InterviewResponseDto ToResponse(InterviewSchedule interview, string googleUrl, string outlookUrl)
    {
        var application = interview.Application;
        var job = application.Job;
        var candidateUser = application.Candidate.User;

        return new InterviewResponseDto(
            InterviewId: interview.Id,
            ApplicationId: application.Id,
            CandidateId: application.CandidateId,
            CandidateName: candidateUser.FullName,
            CandidateEmail: candidateUser.Email,
            JobId: job.Id,
            JobTitle: job.Title,
            CompanyName: job.Organization?.Name ?? string.Empty,
            Title: interview.Title,
            InterviewDate: interview.ScheduledAt,
            DurationMinutes: interview.DurationMinutes,
            EndsAt: interview.ScheduledAt.AddMinutes(interview.DurationMinutes),
            Mode: interview.Mode,
            Location: interview.Location,
            MeetingLink: interview.MeetingLink,
            Notes: interview.Notes,
            Status: interview.Status,
            CreatedAt: interview.CreatedAt,
            GoogleCalendarUrl: googleUrl,
            OutlookCalendarUrl: outlookUrl);
    }

    private async Task<Recruiter> GetRecruiterAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Repository<Recruiter>().FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken)
           ?? throw new ForbiddenException("No recruiter profile is associated with this account.");
}
