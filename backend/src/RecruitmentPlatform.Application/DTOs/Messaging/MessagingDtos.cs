using RecruitmentPlatform.Application.DTOs.Common;
using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.DTOs.Messaging;

// ----- Notifications -----

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    string? Link,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public class NotificationQuery : PaginationQuery
{
    public bool? UnreadOnly { get; set; }
}

// ----- Messaging -----

public record SendMessageRequest(Guid RecipientUserId, string? Subject, string Body);

public record MessageDto(
    Guid Id,
    Guid SenderUserId,
    string SenderName,
    Guid RecipientUserId,
    string? Subject,
    string Body,
    bool IsRead,
    bool IsMine,
    DateTime SentAt,
    DateTime? ReadAt);

public record ConversationDto(
    Guid OtherUserId,
    string OtherUserName,
    string? LastMessage,
    DateTime LastMessageAt,
    int UnreadCount);
