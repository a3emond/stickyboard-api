using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;                  // for User.GetUserId()
using StickyBoard.Api.DTOs;                    // ApiResponseDto + Section DTOs
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class SectionsController : ControllerBase
{
    private readonly SectionService _service;

    public SectionsController(SectionService service)
    {
        _service = service;
    }

    // ------------------------------------------------------------
    // LIST SECTIONS FOR TAB
    // GET /api/sections/tab/{tabId}
    // ------------------------------------------------------------
    [HttpGet("tab/{tabId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<SectionDto>>>> GetForTab(Guid tabId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<SectionDto>>.Fail("Invalid or missing token."));

        var sections = await _service.GetForTabAsync(userId, tabId, ct);
        return Ok(ApiResponseDto<IEnumerable<SectionDto>>.Ok(sections));
    }

    // ------------------------------------------------------------
    // CREATE SECTION
    // POST /api/sections
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<object>>> Create([FromBody] SectionCreateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var id = await _service.CreateAsync(userId, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { id }));
    }

    // ------------------------------------------------------------
    // UPDATE SECTION (title/layout only)
    // PUT /api/sections/{id}
    // ------------------------------------------------------------
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid id, [FromBody] SectionUpdateDto dto, CancellationToken ct)
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
    // MOVE SECTION (reparent/reorder)
    // PUT /api/sections/{id}/move
    // body: { "parentSectionId": "uuid|null", "newPosition": int }
    // ------------------------------------------------------------
    [HttpPut("{id:guid}/move")]
    public async Task<ActionResult<ApiResponseDto<object>>> Move(Guid id, [FromBody] SectionMoveDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var ok = await _service.MoveAsync(userId, id, dto, ct);
        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Section not found."));
    }

    // ------------------------------------------------------------
    // DELETE SECTION (soft delete; cascade handled by repo/db)
    // DELETE /api/sections/{id}
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
            : NotFound(ApiResponseDto<object>.Fail("Section not found."));
    }
}
