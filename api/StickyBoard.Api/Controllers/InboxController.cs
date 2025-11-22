using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Services.SocialAndMessaging;
using StickyBoard.Api.Services.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class InboxController : ControllerBase
{
    private readonly IInboxMessageService _inbox;

    public InboxController(InboxMessageService inbox)
    {
        _inbox = inbox;
    }

    // ------------------------------------------------------------
    // SEND DIRECT MESSAGE
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<InboxMessageDto>>> Send(
        InboxMessageCreateDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<InboxMessageDto>.Fail("Invalid or missing token."));

        var msg = await _inbox.SendAsync(userId, dto, ct);
        return Ok(ApiResponseDto<InboxMessageDto>.Ok(msg));
    }

    // ------------------------------------------------------------
    // GET MY INBOX
    // ------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<InboxMessageDto>>>> GetMyInbox(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<InboxMessageDto>>.Fail("Invalid or missing token."));

        var list = await _inbox.GetForUserAsync(userId, ct);
        return Ok(ApiResponseDto<IEnumerable<InboxMessageDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // MARK AS READ
    // ------------------------------------------------------------
    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponseDto<object>>> MarkAsRead(Guid id, CancellationToken ct)
    {
        var ok = await _inbox.MarkAsReadAsync(id, ct);

        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Message not found or already read."));
    }
}
