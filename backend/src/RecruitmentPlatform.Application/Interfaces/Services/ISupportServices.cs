using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Candidates;
using RecruitmentPlatform.Application.DTOs.Common;
using RecruitmentPlatform.Application.DTOs.Messaging;

namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>Writes audit-log entries for security- and data-relevant actions.</summary>
public interface IAuditService
{
    Task LogAsync(string action, string? entityName = null, string? entityId = null, string? details = null,
        Guid? userId = null, string? ipAddress = null, string? userAgent = null, int? statusCode = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Creates and reads in-app notifications (and, later, fan-out to email/SMS).</summary>
public interface INotificationService
{
    Task NotifyAsync(Guid userId, string title, string message, Domain.Enums.NotificationType type,
        string? link = null, CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationDto>> GetForUserAsync(Guid userId, NotificationQuery query, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>Direct messaging between users (e.g. recruiter ↔ candidate).</summary>
public interface IMessageService
{
    Task<MessageDto> SendAsync(Guid senderUserId, SendMessageRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns the message thread with another user and marks incoming messages read.</summary>
    Task<PagedResult<MessageDto>> GetThreadAsync(Guid userId, Guid otherUserId, PaginationQuery query, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>Handles resume/certificate document upload and retrieval for candidates.</summary>
public interface IDocumentService
{
    Task<ResumeDto> UploadResumeAsync(Guid userId, Stream content, string fileName, string contentType, long length, bool makePrimary, CancellationToken cancellationToken = default);

    Task<(Stream content, string contentType, string fileName)> DownloadResumeAsync(Guid userId, Guid resumeId, CancellationToken cancellationToken = default);

    Task DeleteResumeAsync(Guid userId, Guid resumeId, CancellationToken cancellationToken = default);

    Task SetPrimaryResumeAsync(Guid userId, Guid resumeId, CancellationToken cancellationToken = default);
}
