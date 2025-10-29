using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Sections;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/boards/{boardId:guid}/[controller]")]
    [Authorize]
    public sealed class SectionsController : ControllerBase
    {
        private readonly SectionService _service;

        public SectionsController(SectionService service)
        {
            _service = service;
        }

        // ------------------------------------------------------------
        // READ
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<SectionDto>>>> GetByBoard(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<SectionDto>>.Fail("Invalid or missing token."));

            var items = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(ApiResponseDto<IEnumerable<SectionDto>>.Ok(items));
        }

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> Create(Guid boardId, [FromBody] CreateSectionDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _service.CreateAsync(userId, boardId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        // ------------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------------
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid boardId, Guid id, [FromBody] UpdateSectionDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.UpdateAsync(userId, id, dto, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Section not found."));
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid boardId, Guid id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.DeleteAsync(userId, id, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Section not found."));
        }

        // ------------------------------------------------------------
        // REORDER
        // ------------------------------------------------------------
        [HttpPost("reorder")]
        public async Task<ActionResult<ApiResponseDto<object>>> Reorder(Guid boardId, [FromBody] IEnumerable<ReorderSectionDto> updates, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var count = await _service.ReorderAsync(userId, boardId, updates, ct);
            return Ok(ApiResponseDto<object>.Ok(new { updated = count }));
        }
    }
}
