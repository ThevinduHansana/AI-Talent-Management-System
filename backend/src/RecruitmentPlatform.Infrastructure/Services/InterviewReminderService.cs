using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;
using RecruitmentPlatform.Infrastructure.Data;

namespace RecruitmentPlatform.Infrastructure.Services;

/// <summary>
/// Background worker that sends automated interview reminders at the configured lead times
/// (default: 24 hours and 1 hour before the interview).
/// <para>
/// Reminders are raised through <see cref="INotificationService"/> rather than by calling the
/// email/SMS channels directly, so they reuse the same fan-out, routing and templating as every
/// other notification instead of duplicating that logic.
/// </para>
/// </summary>
public class InterviewReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReminderSettings _settings;
    private readonly ILogger<InterviewReminderService> _logger;

    public InterviewReminderService(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationSettings> options,
        ILogger<InterviewReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = options.Value.Reminders;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Interview reminders are disabled; worker will not run.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.PollIntervalMinutes));
        _logger.LogInformation(
            "Interview reminder worker started (every {Interval}, lead times {First}m and {Second}m).",
            interval, _settings.FirstReminderMinutes, _settings.SecondReminderMinutes);

        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A failed scan must never kill the worker — log and wait for the next tick.
                _logger.LogError(ex, "Interview reminder scan failed; will retry on the next interval.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            return await timer.WaitForNextTickAsync(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>Scans for interviews due a reminder and dispatches them. Internal for testability.</summary>
    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await DispatchAsync(_settings.FirstReminderMinutes, isFinal: false, cancellationToken);
        await DispatchAsync(_settings.SecondReminderMinutes, isFinal: true, cancellationToken);
    }

    private async Task DispatchAsync(int leadMinutes, bool isFinal, CancellationToken cancellationToken)
    {
        if (leadMinutes <= 0)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var horizon = now.AddMinutes(leadMinutes);

        // Only interviews still ahead of us: once the start time passes, a reminder is noise.
        var query = db.Set<InterviewSchedule>()
            .Include(i => i.Application).ThenInclude(a => a.Job)
            .Include(i => i.Application).ThenInclude(a => a.Candidate)
            .Where(i => i.Status == InterviewStatus.Scheduled
                        && i.ScheduledAt > now
                        && i.ScheduledAt <= horizon);

        query = isFinal
            ? query.Where(i => i.SecondReminderSentAt == null)
            : query.Where(i => i.FirstReminderSentAt == null);

        var due = await query
            .OrderBy(i => i.ScheduledAt)
            .Take(Math.Max(1, _settings.BatchSize))
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Sending {Count} {Stage} interview reminder(s).",
            due.Count, isFinal ? "final" : "early");

        foreach (var interview in due)
        {
            var jobTitle = interview.Application?.Job?.Title ?? "your role";
            var lead = Describe(interview.ScheduledAt - now);
            var title = $"Reminder: interview in {lead}";
            var where = BuildLocation(interview);
            var body = $"Your interview for \"{jobTitle}\" starts at {interview.ScheduledAt:f} UTC ({lead} from now).{where}";

            // Candidate.
            if (interview.Application?.Candidate is { } candidate)
            {
                await NotifySafelyAsync(notifications, candidate.UserId, title, body,
                    $"/candidate/applications/{interview.ApplicationId}", cancellationToken);
            }

            // Interviewer, when one is assigned.
            if (_settings.NotifyInterviewer && interview.InterviewerUserId is { } interviewerUserId)
            {
                await NotifySafelyAsync(notifications, interviewerUserId, title,
                    $"You are interviewing for \"{jobTitle}\" at {interview.ScheduledAt:f} UTC ({lead} from now).{where}",
                    $"/recruiter/jobs/{interview.Application?.JobId}/pipeline", cancellationToken);
            }

            // Stamped after the attempt rather than only on success. A reminder is time-boxed: if
            // delivery is broken, retrying every poll would spam the recipient the moment it
            // recovers, and a 24h reminder delivered late has no value anyway. Failures are logged.
            if (isFinal)
            {
                interview.SecondReminderSentAt = DateTime.UtcNow;
            }
            else
            {
                interview.FirstReminderSentAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifySafelyAsync(INotificationService notifications, Guid userId, string title,
        string body, string? link, CancellationToken cancellationToken)
    {
        try
        {
            await notifications.NotifyAsync(userId, title, body, NotificationType.Interview, link, cancellationToken);
        }
        catch (Exception ex)
        {
            // One bad recipient must not abort the whole batch.
            _logger.LogError(ex, "Failed to send interview reminder to user {UserId}.", userId);
        }
    }

    private static string BuildLocation(InterviewSchedule interview)
    {
        if (!string.IsNullOrWhiteSpace(interview.MeetingLink))
        {
            return $" Join: {interview.MeetingLink}";
        }

        return !string.IsNullOrWhiteSpace(interview.Location)
            ? $" Location: {interview.Location}"
            : string.Empty;
    }

    private static string Describe(TimeSpan remaining)
    {
        if (remaining.TotalHours >= 1.5)
        {
            return $"{Math.Round(remaining.TotalHours)} hours";
        }

        return remaining.TotalMinutes >= 2
            ? $"{Math.Round(remaining.TotalMinutes)} minutes"
            : "a few moments";
    }
}
