using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Common;
using RecruitmentPlatform.Application.DTOs.Messaging;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Direct user-to-user messaging. Conversations are derived from the message history; sending a
/// message also raises an in-app notification for the recipient.
/// </summary>
public class MessageService : IMessageService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;

    public MessageService(IUnitOfWork uow, INotificationService notifications)
    {
        _uow = uow;
        _notifications = notifications;
    }

    public async Task<MessageDto> SendAsync(Guid senderUserId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RecipientUserId == senderUserId)
        {
            throw new ValidationException("RecipientUserId", "You cannot message yourself.");
        }
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ValidationException("Body", "Message body is required.");
        }

        var recipient = await _uow.Users.GetByIdAsync(request.RecipientUserId, cancellationToken);
        if (recipient is null || !recipient.IsActive)
        {
            throw new NotFoundException("Recipient", request.RecipientUserId);
        }

        var sender = await _uow.Users.GetByIdAsync(senderUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), senderUserId);

        var message = new Message
        {
            SenderUserId = senderUserId,
            RecipientUserId = request.RecipientUserId,
            Subject = request.Subject,
            Body = request.Body.Trim(),
        };
        await _uow.Repository<Message>().AddAsync(message, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var senderName = $"{sender.FirstName} {sender.LastName}".Trim();
        await _notifications.NotifyAsync(recipient.Id, "New message",
            $"You have a new message from {senderName}.", NotificationType.Message,
            $"/messages/{senderUserId}", cancellationToken);

        return new MessageDto(message.Id, senderUserId, senderName, request.RecipientUserId,
            message.Subject, message.Body, message.IsRead, true, message.CreatedAt, message.ReadAt);
    }

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var messages = await _uow.Repository<Message>().Query()
            .Where(m => m.SenderUserId == userId || m.RecipientUserId == userId)
            .Select(m => new { m.SenderUserId, m.RecipientUserId, m.Body, m.CreatedAt, m.IsRead })
            .ToListAsync(cancellationToken);

        var conversations = messages
            .GroupBy(m => m.SenderUserId == userId ? m.RecipientUserId : m.SenderUserId)
            .Select(g =>
            {
                var last = g.OrderByDescending(m => m.CreatedAt).First();
                var unread = g.Count(m => m.RecipientUserId == userId && !m.IsRead);
                return new { OtherUserId = g.Key, last.Body, last.CreatedAt, Unread = unread };
            })
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var otherIds = conversations.Select(c => c.OtherUserId).ToList();
        var names = (await _uow.Users.Query()
                .Where(u => otherIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync(cancellationToken))
            .ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

        return conversations
            .Select(c => new ConversationDto(
                c.OtherUserId, names.GetValueOrDefault(c.OtherUserId, "Unknown user"),
                c.Body, c.CreatedAt, c.Unread))
            .ToList();
    }

    public async Task<PagedResult<MessageDto>> GetThreadAsync(Guid userId, Guid otherUserId, PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var baseQuery = _uow.Repository<Message>().Query()
            .Where(m => (m.SenderUserId == userId && m.RecipientUserId == otherUserId)
                        || (m.SenderUserId == otherUserId && m.RecipientUserId == userId));

        var total = await baseQuery.CountAsync(cancellationToken);

        var names = (await _uow.Users.Query()
                .Where(u => u.Id == userId || u.Id == otherUserId)
                .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
                .ToListAsync(cancellationToken))
            .ToDictionary(u => u.Id, u => u.Name.Trim());

        var messages = await baseQuery
            .OrderBy(m => m.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        // Mark the incoming messages in this thread as read.
        var unread = await _uow.Repository<Message>().Query().AsTracking()
            .Where(m => m.SenderUserId == otherUserId && m.RecipientUserId == userId && !m.IsRead)
            .ToListAsync(cancellationToken);
        if (unread.Count > 0)
        {
            foreach (var m in unread) { m.IsRead = true; m.ReadAt = DateTime.UtcNow; }
            await _uow.SaveChangesAsync(cancellationToken);
        }

        var items = messages.Select(m => new MessageDto(
            m.Id, m.SenderUserId, names.GetValueOrDefault(m.SenderUserId, "Unknown"),
            m.RecipientUserId, m.Subject, m.Body, m.IsRead || m.RecipientUserId == userId,
            m.SenderUserId == userId, m.CreatedAt, m.ReadAt)).ToList();

        return new PagedResult<MessageDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _uow.Repository<Message>().CountAsync(m => m.RecipientUserId == userId && !m.IsRead, cancellationToken);
}
