using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A scheduled interview tied to an application, optionally synced to an external calendar.
/// </summary>
public class InterviewSchedule : BaseEntity
{
    public Guid ApplicationId { get; set; }

    public JobApplication Application { get; set; } = null!;

    public Guid ScheduledByUserId { get; set; }

    public User ScheduledByUser { get; set; } = null!;

    public Guid? InterviewerUserId { get; set; }

    public User? InterviewerUser { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; }

    public int DurationMinutes { get; set; } = 60;

    public InterviewMode Mode { get; set; } = InterviewMode.Video;

    public string? Location { get; set; }

    public string? MeetingLink { get; set; }

    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

    public string? Notes { get; set; }

    /// <summary>External calendar event id (Google/Outlook) when synced.</summary>
    public string? CalendarEventId { get; set; }

    /// <summary>
    /// Stable RFC 5545 UID for the generated .ics event. Persisted rather than regenerated so a
    /// re-sent invitation updates the existing entry in the attendee's calendar instead of
    /// creating a duplicate.
    /// </summary>
    public string? CalendarUid { get; set; }

    /// <summary>
    /// Incremented on every reschedule. RFC 5545 requires a higher SEQUENCE for a calendar client
    /// to accept an updated event over the one it already stored.
    /// </summary>
    public int CalendarSequence { get; set; }

    /// <summary>
    /// When the early (default: 24h) automated reminder was delivered. Persisted rather than
    /// derived so a restarted or repeatedly-polling worker cannot send the same reminder twice.
    /// </summary>
    public DateTime? FirstReminderSentAt { get; set; }

    /// <summary>When the final (default: 1h) automated reminder was delivered.</summary>
    public DateTime? SecondReminderSentAt { get; set; }

    public ICollection<InterviewFeedback> Feedbacks { get; set; } = new List<InterviewFeedback>();
}
