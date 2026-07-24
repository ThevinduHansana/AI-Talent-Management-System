using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.DTOs.Recruiter;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Schedules and manages interviews for a recruiter's applications, syncing to the external
/// calendar via <see cref="ICalendarService"/> and notifying the candidate.
/// </summary>
public class InterviewService : IInterviewService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICalendarService _calendar;
    private readonly INotificationService _notifications;
    private readonly IInterviewInvitationService _invitations;
    private readonly IAuditService _audit;

    public InterviewService(IUnitOfWork uow, IMapper mapper, ICalendarService calendar,
        INotificationService notifications, IInterviewInvitationService invitations, IAuditService audit)
    {
        _uow = uow;
        _mapper = mapper;
        _calendar = calendar;
        _notifications = notifications;
        _invitations = invitations;
        _audit = audit;
    }

    public async Task<InterviewDto> ScheduleAsync(Guid userId, ScheduleInterviewRequest request, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);

        var application = await _uow.Applications.Query().AsTracking()
            .Include(a => a.Job)
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.Job.RecruiterId == recruiter.Id, cancellationToken)
            ?? throw new NotFoundException("Application", request.ApplicationId);

        if (application.Status is ApplicationStatus.Rejected or ApplicationStatus.Withdrawn or ApplicationStatus.Hired)
        {
            throw new ConflictException($"Cannot schedule an interview for an application in '{application.Status}' state.");
        }

        var scheduledAt = DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc);
        var duration = request.DurationMinutes < 15 ? 30 : request.DurationMinutes;

        // Sync to the external calendar (abstraction: local/Google/Outlook are interchangeable).
        var attendees = new List<string> { application.Candidate.User.Email };
        var eventId = await _calendar.CreateEventAsync(new CalendarEvent(
            request.Title, $"Interview for {application.Job.Title}", scheduledAt,
            scheduledAt.AddMinutes(duration), request.Location, attendees), cancellationToken);

        var interview = new InterviewSchedule
        {
            ApplicationId = application.Id,
            ScheduledByUserId = userId,
            InterviewerUserId = request.InterviewerUserId,
            Title = request.Title.Trim(),
            ScheduledAt = scheduledAt,
            DurationMinutes = duration,
            Mode = request.Mode,
            Location = request.Location,
            MeetingLink = request.MeetingLink,
            Status = InterviewStatus.Scheduled,
            Notes = request.Notes,
            CalendarEventId = eventId,
            // Stable UID so a later reschedule updates the candidate's existing calendar entry.
            CalendarUid = $"{Guid.NewGuid():N}@getcareers",
            CalendarSequence = 0,
        };
        await _uow.Repository<InterviewSchedule>().AddAsync(interview, cancellationToken);

        // Advance the application in the pipeline.
        if (application.Status is ApplicationStatus.Applied or ApplicationStatus.UnderReview or ApplicationStatus.Shortlisted)
        {
            application.Status = ApplicationStatus.InterviewScheduled;
            application.StatusChangedAt = DateTime.UtcNow;
        }

        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("InterviewScheduled", nameof(InterviewSchedule), interview.Id.ToString(),
            $"Interview scheduled for application {application.Id} at {scheduledAt:u}.", userId, cancellationToken: cancellationToken);

        // Email the calendar invitation (Google/Outlook links + .ics). Shared with the
        // /api/interviews endpoint so both scheduling paths behave identically.
        await _invitations.SendInvitationAsync(interview.Id, isReschedule: false, cancellationToken);

        await _notifications.NotifyAsync(
            application.Candidate.UserId,
            "Interview scheduled",
            $"An interview for \"{application.Job.Title}\" is scheduled for {scheduledAt:g} UTC.",
            NotificationType.Interview,
            $"/candidate/applications/{application.Id}",
            cancellationToken);

        return await ProjectAsync(interview.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<InterviewDto>> GetForJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        return await _uow.Repository<InterviewSchedule>().Query()
            .Where(i => i.Application.JobId == jobId && i.Application.Job.RecruiterId == recruiter.Id)
            .OrderBy(i => i.ScheduledAt)
            .ProjectTo<InterviewDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InterviewDto>> GetUpcomingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        return await _uow.Repository<InterviewSchedule>().Query()
            .Where(i => i.Application.Job.RecruiterId == recruiter.Id
                        && i.Status == InterviewStatus.Scheduled
                        && i.ScheduledAt >= DateTime.UtcNow)
            .OrderBy(i => i.ScheduledAt)
            .ProjectTo<InterviewDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid userId, Guid interviewId, CancellationToken cancellationToken = default)
    {
        var recruiter = await GetRecruiterAsync(userId, cancellationToken);
        var interview = await _uow.Repository<InterviewSchedule>().Query().AsTracking()
            .Include(i => i.Application)
            .FirstOrDefaultAsync(i => i.Id == interviewId && i.Application.Job.RecruiterId == recruiter.Id, cancellationToken)
            ?? throw new NotFoundException("Interview", interviewId);

        interview.Status = InterviewStatus.Cancelled;
        // Bumping the sequence is what lets the CANCEL below supersede the invitation the
        // candidate's calendar already stored.
        interview.CalendarSequence += 1;
        if (!string.IsNullOrEmpty(interview.CalendarEventId))
        {
            await _calendar.CancelEventAsync(interview.CalendarEventId, cancellationToken);
        }
        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("InterviewCancelled", nameof(InterviewSchedule), interview.Id.ToString(),
            $"Interview {interview.Id} cancelled.", userId, cancellationToken: cancellationToken);

        await _invitations.SendCancellationAsync(interview.Id, cancellationToken);
    }

    private async Task<InterviewDto> ProjectAsync(Guid interviewId, CancellationToken cancellationToken)
        => await _uow.Repository<InterviewSchedule>().Query()
               .Where(i => i.Id == interviewId)
               .ProjectTo<InterviewDto>(_mapper.ConfigurationProvider)
               .FirstAsync(cancellationToken);

    private async Task<Recruiter> GetRecruiterAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Repository<Recruiter>().FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken)
           ?? throw new ForbiddenException("No recruiter profile is associated with this account.");
}
