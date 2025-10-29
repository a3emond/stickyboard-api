using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Messages;
using StickyBoard.Api.Services;

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

        // ------------------------------------------------------------
        // INBOX
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<MessageDto>>>> Inbox(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<MessageDto>>.Fail("Invalid or missing token."));

            var messages = await _service.GetInboxAsync(userId, ct);
            return Ok(ApiResponseDto<IEnumerable<MessageDto>>.Ok(messages));
        }

        // ------------------------------------------------------------
        // UNREAD COUNT
        // ------------------------------------------------------------
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponseDto<object>>> UnreadCount(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var count = await _service.GetUnreadCountAsync(userId, ct);
            return Ok(ApiResponseDto<object>.Ok(new { count }));
        }

        // ------------------------------------------------------------
        // SEND MESSAGE
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> Send([FromBody] SendMessageDto dto, CancellationToken ct)
        {
            var senderId = User.GetUserId();
            if (senderId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _service.SendAsync(senderId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        // ------------------------------------------------------------
        // UPDATE STATUS
        // ------------------------------------------------------------
        [HttpPut("{id:guid}/status")]
        public async Task<ActionResult<ApiResponseDto<object>>> UpdateStatus(Guid id, [FromBody] UpdateMessageStatusDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.UpdateStatusAsync(userId, id, dto.Status, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Message not found or not accessible."));
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.DeleteAsync(userId, id, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Message not found or not accessible."));
        }
    }
}
