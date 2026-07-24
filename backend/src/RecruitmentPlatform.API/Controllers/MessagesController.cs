using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Common;
using RecruitmentPlatform.Application.DTOs.Messaging;
using RecruitmentPlatform.Application.Interfaces.Services;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>Direct messaging between users for the authenticated user.</summary>
[Authorize]
public class MessagesController : ApiControllerBase
{
    private readonly IMessageService _service;

    public MessagesController(IMessageService service) => _service = service;

    /// <summary>Sends a message to another user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> Send(SendMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.SendAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Lists the user's conversations, most recent first.</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> Conversations(CancellationToken cancellationToken)
        => Ok(await _service.GetConversationsAsync(CurrentUserId, cancellationToken));

    /// <summary>Returns the unread message count across all conversations.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken cancellationToken)
        => Ok(await _service.GetUnreadCountAsync(CurrentUserId, cancellationToken));

    /// <summary>Returns the message thread with another user and marks incoming messages read.</summary>
    [HttpGet("{otherUserId:guid}")]
    [ProducesResponseType(typeof(PagedResult<MessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MessageDto>>> Thread(Guid otherUserId, [FromQuery] PaginationQuery query, CancellationToken cancellationToken)
        => Ok(await _service.GetThreadAsync(CurrentUserId, otherUserId, query, cancellationToken));
}
