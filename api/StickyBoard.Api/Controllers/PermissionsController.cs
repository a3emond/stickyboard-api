using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Permissions;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/boards/{boardId:guid}/[controller]")]
    [Authorize]
    public sealed class PermissionsController : ControllerBase
    {
        private readonly PermissionService _service;

        public PermissionsController(PermissionService service)
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
            if (userId == Guid.Empty)
                return Unauthorized();

            var list = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(list);
        }

        [HttpGet("~/api/users/{userId:guid}/boards")]
        public async Task<IActionResult> GetByUser(Guid userId, CancellationToken ct)
        {
            var list = await _service.GetByUserAsync(userId, ct);
            return Ok(list);
        }

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        [HttpPost]
        public async Task<IActionResult> Add(Guid boardId, [FromBody] GrantPermissionDto dto, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            if (actorId == Guid.Empty)
                return Unauthorized();

            var id = await _service.AddAsync(actorId, boardId, dto, ct);
            return Ok(new { boardId = id });
        }

        [HttpPut("{userId:guid}")]
        public async Task<IActionResult> Update(Guid boardId, Guid userId, [FromBody] UpdatePermissionDto dto, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            if (actorId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.UpdateAsync(actorId, boardId, userId, dto, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{userId:guid}")]
        public async Task<IActionResult> Remove(Guid boardId, Guid userId, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            if (actorId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.RemoveAsync(actorId, boardId, userId, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }
    }
}
