using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Services.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cards/{cardId:guid}/comments")]
public sealed class CardCommentsController : ControllerBase
{
    private readonly ICardCommentService _service;

    public CardCommentsController(ICardCommentService service)
    {
        _service = service;
    }

    // ------------------------------------------------------------
    // GET COMMENTS FOR CARD
    // ------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<CardCommentDto>>>> GetByCard(
        Guid cardId,
        CancellationToken ct)
    {
        var list = await _service.GetByCardAsync(cardId, ct);
        return Ok(ApiResponseDto<IEnumerable<CardCommentDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // GET THREAD (RECURSIVE)
    // ------------------------------------------------------------
    [HttpGet("thread/{rootId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<CardCommentDto>>>> GetThread(
        Guid cardId,
        Guid rootId,
        CancellationToken ct)
    {
        var list = await _service.GetThreadAsync(cardId, rootId, ct);
        return Ok(ApiResponseDto<IEnumerable<CardCommentDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<object>>> Create(
        Guid cardId,
        [FromBody] CardCommentCreateDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(ApiResponseDto<object>.Fail("Content is required."));

        var userId = User.GetUserId();

        var id = await _service.CreateAsync(cardId, userId, dto, ct);

        return Ok(ApiResponseDto<object>.Ok(new { id }));
    }

    // ------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Update(
        Guid cardId,
        Guid id,
        [FromBody] CardCommentUpdateDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(ApiResponseDto<object>.Fail("Content is required."));

        var userId = User.GetUserId();

        var ok = await _service.UpdateAsync(id, userId, dto, ct);

        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Comment not found."));
    }

    // ------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Delete(
        Guid cardId,
        Guid id,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        var ok = await _service.DeleteAsync(id, userId, ct);

        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Comment not found."));
    }
}
