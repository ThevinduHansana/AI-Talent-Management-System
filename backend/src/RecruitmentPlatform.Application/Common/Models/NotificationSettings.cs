using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Common.Models;

/// <summary>
/// Controls how in-app notifications fan out to external channels, and how interview reminders
/// are scheduled. Routing is per <see cref="NotificationType"/> so email volume and (especially)
/// SMS spend stay deliberate rather than "everything, everywhere".
/// </summary>
public class NotificationSettings
{
    public const string SectionName = "Notifications";

    /// <summary>Mirror in-app notifications to email for the types listed in <see cref="EmailTypes"/>.</summary>
    public bool EmailFanoutEnabled { get; set; } = true;

    /// <summary>Mirror in-app notifications to SMS for the types listed in <see cref="SmsTypes"/>.</summary>
    public bool SmsFanoutEnabled { get; set; } = true;

    /// <summary>Notification types that also send an email. Defaults to everything meaningful.</summary>
    public List<NotificationType> EmailTypes { get; set; } = new()
    {
        NotificationType.Application,
        NotificationType.Interview,
        NotificationType.Job,
        NotificationType.System,
    };

    /// <summary>
    /// Notification types that also send an SMS. Deliberately narrow: text messages are intrusive
    /// and billed per segment, so only time-critical interview traffic qualifies by default.
    /// </summary>
    public List<NotificationType> SmsTypes { get; set; } = new()
    {
        NotificationType.Interview,
    };

    public ReminderSettings Reminders { get; set; } = new();
}

/// <summary>Scheduling for automated interview reminders.</summary>
public class ReminderSettings
{
    /// <summary>Master switch for the background reminder worker.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often the worker scans for interviews that are due a reminder.</summary>
    public int PollIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Lead times, in minutes before the interview start, at which reminders fire. Each interview
    /// gets at most one reminder per configured lead time. Defaults to 24 hours and 1 hour.
    /// </summary>
    public int FirstReminderMinutes { get; set; } = 1440;

    public int SecondReminderMinutes { get; set; } = 60;

    /// <summary>Also remind the interviewer, not just the candidate.</summary>
    public bool NotifyInterviewer { get; set; } = true;

    /// <summary>Upper bound on interviews processed per scan, so a backlog cannot stall the worker.</summary>
    public int BatchSize { get; set; } = 100;
}
