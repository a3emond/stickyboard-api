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
public sealed class MessagesController : ControllerBase
{
    private readonly IMessageService _messages;

    public MessagesController(MessageService messages)
    {
        _messages = messages;
    }

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<MessageDto>>> Create(MessageCreateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<MessageDto>.Fail("Invalid or missing token."));

        var msg = await _messages.CreateAsync(userId, dto, ct);
        return Ok(ApiResponseDto<MessageDto>.Ok(msg));
    }

    // ------------------------------------------------------------
    // UPDATE (sender only)
    // ------------------------------------------------------------
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid id, MessageUpdateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var ok = await _messages.UpdateAsync(id, userId, dto, ct);

        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Message not found or not your message."));
    }

    // ------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _messages.DeleteAsync(id, ct);

        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Message not found."));
    }

    // ------------------------------------------------------------
    // GET BY BOARD
    // ------------------------------------------------------------
    [HttpGet("board/{boardId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<MessageDto>>>> GetByBoard(Guid boardId, CancellationToken ct)
    {
        var list = await _messages.GetByBoardAsync(boardId, ct);
        return Ok(ApiResponseDto<IEnumerable<MessageDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // GET BY VIEW
    // ------------------------------------------------------------
    [HttpGet("view/{viewId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<MessageDto>>>> GetByView(Guid viewId, CancellationToken ct)
    {
        var list = await _messages.GetByViewAsync(viewId, ct);
        return Ok(ApiResponseDto<IEnumerable<MessageDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // GET BY ID
    // ------------------------------------------------------------
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<MessageDto>>> Get(Guid id, CancellationToken ct)
    {
        var msg = await _messages.GetAsync(id, ct);

        return msg is not null
            ? Ok(ApiResponseDto<MessageDto>.Ok(msg))
            : NotFound(ApiResponseDto<MessageDto>.Fail("Message not found."));
    }
}
