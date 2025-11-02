using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public sealed class MessagesController : ControllerBase
{
    private readonly MessageService _service;
    public MessagesController(MessageService service) => _service = service;

    private Guid AuthUser() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Inbox(CancellationToken ct)
    {
        var msgs = await _service.GetInboxAsync(AuthUser(), ct);
        return Ok(ApiResponseDto<IEnumerable<MessageDto>>.Ok(msgs));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var count = await _service.GetUnreadCountAsync(AuthUser(), ct);
        return Ok(ApiResponseDto<object>.Ok(new { count }));
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageDto dto, CancellationToken ct)
    {
        var id = await _service.SendAsync(AuthUser(), dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { id }));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateMessageStatusDto dto, CancellationToken ct)
    {
        await _service.UpdateStatusAsync(AuthUser(), id, dto.Status, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(AuthUser(), id, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }
}