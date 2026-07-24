using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Messaging;
using RecruitmentPlatform.Application.Interfaces.Services;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>In-app notifications for the authenticated user.</summary>
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    /// <summary>Lists the user's notifications (optionally unread only).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> Get([FromQuery] NotificationQuery query, CancellationToken cancellationToken)
        => Ok(await _service.GetForUserAsync(CurrentUserId, query, cancellationToken));

    /// <summary>Returns the count of unread notifications.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken cancellationToken)
        => Ok(await _service.GetUnreadCountAsync(CurrentUserId, cancellationToken));

    /// <summary>Marks a single notification as read.</summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _service.MarkAsReadAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }

    /// <summary>Marks all notifications as read.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _service.MarkAllAsReadAsync(CurrentUserId, cancellationToken);
        return NoContent();
    }
}
