using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Activities;
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
        public async Task<IActionResult> GetByBoard(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var list = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(list);
        }

        [HttpGet("cards/{cardId:guid}")]
        public async Task<IActionResult> GetByCard(Guid boardId, Guid cardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var list = await _service.GetByCardAsync(userId, cardId, ct);
            return Ok(list);
        }

        [HttpGet("~/api/activities/recent")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetRecent([FromQuery] int limit = 20, CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            var list = await _service.GetRecentAsync(userId, limit, ct);
            return Ok(list);
        }

        // ----------------------------------------------------------------------
        // CREATE
        // ----------------------------------------------------------------------

        [HttpPost]
        public async Task<IActionResult> Log(Guid boardId, [FromBody] CreateActivityDto dto, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            if (actorId == Guid.Empty)
                return Unauthorized();

            dto.BoardId = boardId;
            var id = await _service.LogAsync(actorId, dto, ct);
            return Ok(new { id });
        }

        // ----------------------------------------------------------------------
        // DELETE (admin only)
        // ----------------------------------------------------------------------

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var ok = await _service.DeleteAsync(id, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }
    }
}
