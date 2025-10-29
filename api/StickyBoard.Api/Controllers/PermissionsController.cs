using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Permissions;
using StickyBoard.Api.Services;

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

        // ------------------------------------------------------------
        // READ
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<PermissionDto>>>> GetByBoard(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<PermissionDto>>.Fail("Invalid or missing token."));

            var list = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(ApiResponseDto<IEnumerable<PermissionDto>>.Ok(list));
        }

        [HttpGet("~/api/users/{userId:guid}/boards")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<PermissionDto>>>> GetByUser(Guid userId, CancellationToken ct)
        {
            var list = await _service.GetByUserAsync(userId, ct);
            return Ok(ApiResponseDto<IEnumerable<PermissionDto>>.Ok(list));
        }

        // ------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> Add(Guid boardId, [FromBody] GrantPermissionDto dto, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            if (actorId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _service.AddAsync(actorId, boardId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        [HttpPut("{userId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid boardId, Guid userId, [FromBody] UpdatePermissionDto dto, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            if (actorId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.UpdateAsync(actorId, boardId, userId, dto, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Permission not found."));
        }

        [HttpDelete("{userId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Remove(Guid boardId, Guid userId, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            if (actorId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.RemoveAsync(actorId, boardId, userId, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Permission not found."));
        }
    }
}
