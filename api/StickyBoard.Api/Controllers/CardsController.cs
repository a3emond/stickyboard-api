using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Cards;
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
    public sealed class CardsController : ControllerBase
    {
        private readonly CardService _service;

        public CardsController(CardService service)
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

            var cards = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(cards);
        }

        [HttpGet("section/{sectionId:guid}")]
        public async Task<IActionResult> GetBySection(Guid sectionId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var cards = await _service.GetBySectionAsync(userId, sectionId, ct);
            return Ok(cards);
        }

        [HttpGet("tab/{tabId:guid}")]
        public async Task<IActionResult> GetByTab(Guid tabId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var cards = await _service.GetByTabAsync(userId, tabId, ct);
            return Ok(cards);
        }

        [HttpGet("assignee/{userId:guid}")]
        public async Task<IActionResult> GetByAssignee(Guid userId, CancellationToken ct)
        {
            var cards = await _service.GetByAssigneeAsync(userId, ct);
            return Ok(cards);
        }

        [HttpGet("board/{boardId:guid}/search")]
        public async Task<IActionResult> Search(Guid boardId, [FromQuery] string q, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var results = await _service.SearchAsync(userId, boardId, q, ct);
            return Ok(results);
        }

        [HttpGet("board/{boardId:guid}/status/{status}")]
        public async Task<IActionResult> GetByStatus(Guid boardId, CardStatus status, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var results = await _service.GetByStatusAsync(userId, boardId, status, ct);
            return Ok(results);
        }

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCardDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var id = await _service.CreateAsync(userId, dto, ct);
            return Ok(new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCardDto dto, CancellationToken ct)
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
        // BULK
        // ----------------------------------------------------------------------

        [HttpPost("board/{boardId:guid}/assign")]
        public async Task<IActionResult> BulkAssign(Guid boardId, [FromBody] BulkAssignDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var count = await _service.BulkAssignAsync(userId, boardId, dto.AssigneeId, dto.CardIds, ct);
            return Ok(new { updated = count });
        }
    }
}
