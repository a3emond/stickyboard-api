using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BoardsController : ControllerBase
{
    private readonly BoardService _service;

    public BoardsController(BoardService service)
    {
        _service = service;
    }

    private Guid CurrentUserId() => User.GetUserId();

    // ------------------------------------------------------------
    // GET: mine
    // ------------------------------------------------------------
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var userId = CurrentUserId();
        var boards = await _service.GetMineAsync(userId, ct);
        return Ok(ApiResponseDto<IEnumerable<BoardDto>>.Ok(boards));
    }

    // ------------------------------------------------------------
    // GET: accessible
    // ------------------------------------------------------------
    [HttpGet("accessible")]
    public async Task<IActionResult> Accessible(CancellationToken ct)
    {
        var userId = CurrentUserId();
        var boards = await _service.GetAccessibleAsync(userId, ct);
        return Ok(ApiResponseDto<IEnumerable<BoardDto>>.Ok(boards));
    }

    // ------------------------------------------------------------
    // GET: search?keyword=abc
    // ------------------------------------------------------------
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword, CancellationToken ct)
    {
        var userId = CurrentUserId();
        var boards = await _service.SearchAccessibleAsync(userId, keyword, ct);
        return Ok(ApiResponseDto<IEnumerable<BoardDto>>.Ok(boards));
    }

    // ------------------------------------------------------------
    // GET: /{id}
    // ------------------------------------------------------------
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var userId = CurrentUserId();
        var board = await _service.GetAsync(userId, id, ct);
        return Ok(ApiResponseDto<BoardDto>.Ok(board));
    }

    // ------------------------------------------------------------
    // POST: create
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BoardCreateDto dto, CancellationToken ct)
    {
        var userId = CurrentUserId();
        var id = await _service.CreateAsync(userId, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { id }));
    }

    // ------------------------------------------------------------
    // PUT: update
    // ------------------------------------------------------------
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BoardUpdateDto dto, CancellationToken ct)
    {
        var userId = CurrentUserId();
        await _service.UpdateAsync(userId, id, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = CurrentUserId();
        await _service.DeleteAsync(userId, id, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // PATCH: rename
    // ------------------------------------------------------------
    [HttpPatch("{id:guid}/rename")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameBoardDto dto, CancellationToken ct)
    {
        var userId = CurrentUserId();
        await _service.RenameAsync(userId, id, dto.Title, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // PATCH: move folder
    // ------------------------------------------------------------
    [HttpPatch("{id:guid}/folder")]
    public async Task<IActionResult> MoveFolder(Guid id, [FromBody] MoveBoardFolderDto dto, CancellationToken ct)
    {
        var userId = CurrentUserId();
        await _service.MoveToFolderAsync(userId, id, dto.FolderId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // PATCH: move org
    // ------------------------------------------------------------
    [HttpPatch("{id:guid}/org")]
    public async Task<IActionResult> MoveOrg(Guid id, [FromBody] MoveBoardOrgDto dto, CancellationToken ct)
    {
        var userId = CurrentUserId();
        await _service.MoveToOrgAsync(userId, id, dto.OrgId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }
}
