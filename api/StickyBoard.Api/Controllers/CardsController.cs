using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Cards;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Services;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.Enums;

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
        public async Task<ActionResult<ApiResponseDto<IEnumerable<CardDto>>>> GetByBoard(Guid boardId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<CardDto>>.Fail("Invalid or missing token."));

            var cards = await _service.GetByBoardAsync(userId, boardId, ct);
            return Ok(ApiResponseDto<IEnumerable<CardDto>>.Ok(cards));
        }

        [HttpGet("section/{sectionId:guid}")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<CardDto>>>> GetBySection(Guid sectionId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<CardDto>>.Fail("Invalid or missing token."));

            var cards = await _service.GetBySectionAsync(userId, sectionId, ct);
            return Ok(ApiResponseDto<IEnumerable<CardDto>>.Ok(cards));
        }

        [HttpGet("tab/{tabId:guid}")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<CardDto>>>> GetByTab(Guid tabId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<CardDto>>.Fail("Invalid or missing token."));

            var cards = await _service.GetByTabAsync(userId, tabId, ct);
            return Ok(ApiResponseDto<IEnumerable<CardDto>>.Ok(cards));
        }

        [HttpGet("assignee/{userId:guid}")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<CardDto>>>> GetByAssignee(Guid userId, CancellationToken ct)
        {
            var cards = await _service.GetByAssigneeAsync(userId, ct);
            return Ok(ApiResponseDto<IEnumerable<CardDto>>.Ok(cards));
        }

        [HttpGet("board/{boardId:guid}/search")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<CardDto>>>> Search(Guid boardId, [FromQuery] string q, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<CardDto>>.Fail("Invalid or missing token."));

            var results = await _service.SearchAsync(userId, boardId, q, ct);
            return Ok(ApiResponseDto<IEnumerable<CardDto>>.Ok(results));
        }

        [HttpGet("board/{boardId:guid}/status/{status}")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<CardDto>>>> GetByStatus(Guid boardId, CardStatus status, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<CardDto>>.Fail("Invalid or missing token."));

            var results = await _service.GetByStatusAsync(userId, boardId, status, ct);
            return Ok(ApiResponseDto<IEnumerable<CardDto>>.Ok(results));
        }

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> Create([FromBody] CreateCardDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _service.CreateAsync(userId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid id, [FromBody] UpdateCardDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _service.UpdateAsync(userId, id, dto, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Card not found."));
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
                : NotFound(ApiResponseDto<object>.Fail("Card not found."));
        }

        // ----------------------------------------------------------------------
        // BULK
        // ----------------------------------------------------------------------

        [HttpPost("board/{boardId:guid}/assign")]
        public async Task<ActionResult<ApiResponseDto<object>>> BulkAssign(Guid boardId, [FromBody] BulkAssignDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var count = await _service.BulkAssignAsync(userId, boardId, dto.AssigneeId, dto.CardIds, ct);
            return Ok(ApiResponseDto<object>.Ok(new { updated = count }));
        }
    }
}
