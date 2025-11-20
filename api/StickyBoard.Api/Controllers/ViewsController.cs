using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Services.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/boards/{boardId:guid}/views")]
[Authorize]
public sealed class ViewsController : ControllerBase
{
    private readonly IViewService _views;

    public ViewsController(IViewService views)
    {
        _views = views;
    }

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<ViewDto>>> Create(
        Guid boardId,
        [FromBody] ViewCreateDto dto,
        CancellationToken ct)
    {
        var view = await _views.CreateAsync(boardId, dto, ct);
        return Ok(ApiResponseDto<ViewDto>.Ok(view));
    }

    // ------------------------------------------------------------
    // GET FOR BOARD
    // ------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<ViewDto>>>> GetForBoard(
        Guid boardId,
        CancellationToken ct)
    {
        var list = await _views.GetForBoardAsync(boardId, ct);
        return Ok(ApiResponseDto<IEnumerable<ViewDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------
    [HttpPut("{viewId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Update(
        Guid boardId,
        Guid viewId,
        [FromBody] ViewUpdateDto dto,
        CancellationToken ct)
    {
        await _views.UpdateAsync(viewId, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------
    [HttpDelete("{viewId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Delete(
        Guid boardId,
        Guid viewId,
        CancellationToken ct)
    {
        await _views.DeleteAsync(viewId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }
}