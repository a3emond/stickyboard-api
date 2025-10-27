using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Messages;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class MessagesController : ControllerBase
    {
        private readonly MessageService _service;

        public MessagesController(MessageService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Inbox(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var messages = await _service.GetInboxAsync(userId, ct);
            return Ok(messages);
        }
        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var count = await _service.GetUnreadCountAsync(userId, ct);
            return Ok(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendMessageDto dto, CancellationToken ct)
        {
            var senderId = User.GetUserId();
            if (senderId == Guid.Empty)
                return Unauthorized();

            var id = await _service.SendAsync(senderId, dto, ct);
            return Ok(new { id });
        }

        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateMessageStatusDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.UpdateStatusAsync(userId, id, dto.Status, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.DeleteAsync(userId, id, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }
    }
}
