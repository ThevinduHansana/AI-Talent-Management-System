using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Common.Email;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Messaging;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Creates in-app notifications and fans them out to email/SMS. Every caller that already raised
/// an in-app notification gains external delivery for free — routing per notification type is
/// configured in <see cref="NotificationSettings"/>.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly ISmsService _sms;
    private readonly NotificationSettings _settings;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork uow,
        IEmailService email,
        ISmsService sms,
        IOptions<NotificationSettings> settings,
        IOptions<EmailSettings> emailSettings,
        ILogger<NotificationService> logger)
    {
        _uow = uow;
        _email = email;
        _sms = sms;
        _settings = settings.Value;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task NotifyAsync(Guid userId, string title, string message, NotificationType type,
        string? link = null, CancellationToken cancellationToken = default)
    {
        await _uow.Repository<Notification>().AddAsync(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        await FanOutAsync(userId, title, message, type, link, cancellationToken);
    }

    /// <summary>
    /// Mirrors a notification to the external channels enabled for its type.
    /// <para>
    /// Delivery failures are logged and swallowed on purpose: the in-app notification is already
    /// committed, and a flaky mail server must never turn a successful business operation (a
    /// status change, a booked interview) into a failed HTTP request.
    /// </para>
    /// </summary>
    private async Task FanOutAsync(Guid userId, string title, string message, NotificationType type,
        string? link, CancellationToken cancellationToken)
    {
        var wantsEmail = _settings.EmailFanoutEnabled && _settings.EmailTypes.Contains(type);
        var wantsSms = _settings.SmsFanoutEnabled && _settings.SmsTypes.Contains(type);

        if (!wantsEmail && !wantsSms)
        {
            return;
        }

        try
        {
            var recipient = await _uow.Repository<User>().Query()
                .Where(u => u.Id == userId && u.IsActive)
                .Select(u => new { u.Email, u.PhoneNumber, u.FirstName })
                .FirstOrDefaultAsync(cancellationToken);

            if (recipient is null)
            {
                return;
            }

            if (wantsEmail)
            {
                await _email.SendAsync(
                    recipient.Email,
                    title,
                    EmailTemplates.Notification(title, message, ToAbsoluteUrl(link)),
                    cancellationToken);
            }

            if (wantsSms && !string.IsNullOrWhiteSpace(recipient.PhoneNumber))
            {
                await _sms.SendAsync(recipient.PhoneNumber, $"{title}: {message}", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification fan-out failed for user {UserId} ({Type}).", userId, type);
        }
    }

    /// <summary>
    /// Notification links are client-side routes ("/candidate/applications/..."), which are dead
    /// text in an email. Rebase them onto the public app origin so they are clickable.
    /// </summary>
    private string? ToAbsoluteUrl(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return null;
        }

        return Uri.TryCreate(link, UriKind.Absolute, out _)
            ? link
            : $"{_emailSettings.AppBaseUrl.TrimEnd('/')}/{link.TrimStart('/')}";
    }

    public async Task<PagedResult<NotificationDto>> GetForUserAsync(Guid userId, NotificationQuery query, CancellationToken cancellationToken = default)
    {
        var q = _uow.Repository<Notification>().Query().Where(n => n.UserId == userId);
        if (query.UnreadOnly == true) q = q.Where(n => !n.IsRead);

        q = q.OrderByDescending(n => n.CreatedAt);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Type, n.Link, n.IsRead, n.CreatedAt, n.ReadAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _uow.Repository<Notification>().CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _uow.Repository<Notification>()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), notificationId);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _uow.Repository<Notification>()
            .ListAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
        if (unread.Count == 0) return;

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
