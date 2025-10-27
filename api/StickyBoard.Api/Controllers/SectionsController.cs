using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Sections;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;

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

        [HttpGet]
        public async Task<IActionResult> GetByBoard(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var items = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid boardId, [FromBody] CreateSectionDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var id = await _service.CreateAsync(userId, boardId, dto, ct);
            return Ok(new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid boardId, Guid id, [FromBody] UpdateSectionDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.UpdateAsync(userId, id, dto, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid boardId, Guid id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.DeleteAsync(userId, id, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        [HttpPost("reorder")]
        public async Task<IActionResult> Reorder(Guid boardId, [FromBody] IEnumerable<ReorderSectionDto> updates, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var count = await _service.ReorderAsync(userId, boardId, updates, ct);
            return Ok(new { updated = count });
        }
    }
}
