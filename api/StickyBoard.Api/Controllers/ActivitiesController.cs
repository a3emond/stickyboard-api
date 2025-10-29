using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Activities;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/boards/{boardId:guid}/[controller]")]
    [Authorize]
    public sealed class ActivitiesController : ControllerBase
    {
        private readonly ActivityService _service;

        public ActivitiesController(ActivityService service)
        {
            _service = service;
        }

        // ----------------------------------------------------------------------
        // READ
        // ----------------------------------------------------------------------

        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<ActivityDto>>>> GetByBoard(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var list = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(ApiResponseDto<IEnumerable<ActivityDto>>.Ok(list));
        }

        [HttpGet("cards/{cardId:guid}")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<ActivityDto>>>> GetByCard(Guid boardId, Guid cardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var list = await _service.GetByCardAsync(userId, cardId, ct);
            return Ok(ApiResponseDto<IEnumerable<ActivityDto>>.Ok(list));
        }

        [HttpGet("~/api/activities/recent")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<ActivityDto>>>> GetRecent([FromQuery] int limit = 20, CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            var list = await _service.GetRecentAsync(userId, limit, ct);
            return Ok(ApiResponseDto<IEnumerable<ActivityDto>>.Ok(list));
        }

        // ----------------------------------------------------------------------
        // CREATE
        // ----------------------------------------------------------------------

        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> Log(Guid boardId, [FromBody] CreateActivityDto dto, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            if (actorId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            dto.BoardId = boardId;
            var id = await _service.LogAsync(actorId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        // ----------------------------------------------------------------------
        // DELETE (admin only)
        // ----------------------------------------------------------------------

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid id, CancellationToken ct)
        {
            var ok = await _service.DeleteAsync(id, ct);
            if (!ok)
                return NotFound(ApiResponseDto<object>.Fail("Activity not found."));

            return Ok(ApiResponseDto<object>.Ok(new { deleted = true }));
        }
    }
}
