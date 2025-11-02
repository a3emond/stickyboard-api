using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;                  // for User.GetUserId()
using StickyBoard.Api.DTOs;                    // ApiResponseDto + Tab DTOs
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

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
    // LIST TABS FOR BOARD
    // GET /api/tabs/board/{boardId}
    // ------------------------------------------------------------
    [HttpGet("board/{boardId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<TabDto>>>> GetForBoard(Guid boardId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<TabDto>>.Fail("Invalid or missing token."));

        var tabs = await _service.GetForBoardAsync(userId, boardId, ct);
        return Ok(ApiResponseDto<IEnumerable<TabDto>>.Ok(tabs));
    }

    // ------------------------------------------------------------
    // CREATE TAB
    // POST /api/tabs
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<object>>> Create([FromBody] TabCreateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var id = await _service.CreateAsync(userId, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { id }));
    }

    // ------------------------------------------------------------
    // UPDATE TAB (title/type/layout)
    // PUT /api/tabs/{id}
    // ------------------------------------------------------------
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid id, [FromBody] TabUpdateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var ok = await _service.UpdateAsync(userId, id, dto, ct);
        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Tab not found."));
    }

    // ------------------------------------------------------------
    // MOVE TAB (reorder)
    // PUT /api/tabs/{id}/move
    // body: { "newPosition": int }
    // ------------------------------------------------------------
    [HttpPut("{id:guid}/move")]
    public async Task<ActionResult<ApiResponseDto<object>>> Move(Guid id, [FromBody] TabMoveDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var ok = await _service.MoveAsync(userId, id, dto.NewPosition, ct);
        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Tab not found."));
    }

    // ------------------------------------------------------------
    // DELETE TAB (soft delete cascade handled in repo/db)
    // DELETE /api/tabs/{id}
    // ------------------------------------------------------------
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
}
