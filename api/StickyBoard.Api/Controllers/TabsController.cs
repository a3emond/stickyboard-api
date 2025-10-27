using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Tabs;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        // ----------------------------------------------------------------------
        // READ
        // ----------------------------------------------------------------------

        [HttpGet("board/{boardId:guid}")]
        public async Task<IActionResult> GetByBoard(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var items = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(items);
        }

        [HttpGet("section/{sectionId:guid}")]
        public async Task<IActionResult> GetBySection(Guid sectionId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var items = await _service.GetBySectionAsync(userId, sectionId, ct);
            return Ok(items);
        }

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        [HttpPost("board/{boardId:guid}")]
        public async Task<IActionResult> Create(Guid boardId, [FromBody] CreateTabDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var id = await _service.CreateAsync(userId, boardId, dto, ct);
            return Ok(new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTabDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var ok = await _service.UpdateAsync(userId, id, dto, ct);
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

        // ----------------------------------------------------------------------
        // REORDER
        // ----------------------------------------------------------------------

        [HttpPost("board/{boardId:guid}/reorder")]
        public async Task<IActionResult> ReorderBoard(Guid boardId, [FromBody] IEnumerable<ReorderTabDto> updates, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var count = await _service.ReorderAsync(userId, TabScope.board, boardId, updates, ct);
            return Ok(new { updated = count });
        }

        [HttpPost("section/{sectionId:guid}/reorder")]
        public async Task<IActionResult> ReorderSection(Guid sectionId, [FromBody] IEnumerable<ReorderTabDto> updates, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var count = await _service.ReorderAsync(userId, TabScope.section, sectionId, updates, ct);
            return Ok(new { updated = count });
        }
    }
}
