using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Tabs;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class TabsController : ControllerBase
    {
        private readonly TabService _service;

        public TabsController(TabService service)
        {
            _service = service;
        }

        // ------------------------------------------------------------
        // READ
        // ------------------------------------------------------------
        [HttpGet("board/{boardId:guid}")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<TabDto>>>> GetByBoard(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<TabDto>>.Fail("Invalid or missing token."));

            var items = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(ApiResponseDto<IEnumerable<TabDto>>.Ok(items));
        }

        [HttpGet("section/{sectionId:guid}")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<TabDto>>>> GetBySection(Guid sectionId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<TabDto>>.Fail("Invalid or missing token."));

            var items = await _service.GetBySectionAsync(userId, sectionId, ct);
            return Ok(ApiResponseDto<IEnumerable<TabDto>>.Ok(items));
        }

        // ------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ------------------------------------------------------------
        [HttpPost("board/{boardId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Create(Guid boardId, [FromBody] CreateTabDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _service.CreateAsync(userId, boardId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid id, [FromBody] UpdateTabDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.UpdateAsync(userId, id, dto, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Tab not found."));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.DeleteAsync(userId, id, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Tab not found."));
        }

        // ------------------------------------------------------------
        // REORDER
        // ------------------------------------------------------------
        [HttpPost("board/{boardId:guid}/reorder")]
        public async Task<ActionResult<ApiResponseDto<object>>> ReorderBoard(Guid boardId, [FromBody] IEnumerable<ReorderTabDto> updates, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var count = await _service.ReorderAsync(userId, TabScope.board, boardId, updates, ct);
            return Ok(ApiResponseDto<object>.Ok(new { updated = count }));
        }

        [HttpPost("section/{sectionId:guid}/reorder")]
        public async Task<ActionResult<ApiResponseDto<object>>> ReorderSection(Guid sectionId, [FromBody] IEnumerable<ReorderTabDto> updates, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var count = await _service.ReorderAsync(userId, TabScope.section, sectionId, updates, ct);
            return Ok(ApiResponseDto<object>.Ok(new { updated = count }));
        }
    }
}
