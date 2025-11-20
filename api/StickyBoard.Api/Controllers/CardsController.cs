using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Services.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/boards/{boardId:guid}/cards")]
public sealed class CardsController : ControllerBase
{
    private readonly ICardService _cards;

    public CardsController(ICardService cards)
    {
        _cards = cards;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<CardDto>>> Create(Guid boardId, [FromBody] CardCreateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        dto.BoardId = boardId;

        var card = await _cards.CreateAsync(userId, dto, ct);
        return Ok(ApiResponseDto<CardDto>.Ok(card));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<CardDto>>>> GetByBoard(Guid boardId, CancellationToken ct)
    {
        var list = await _cards.GetByBoardAsync(boardId, ct);
        return Ok(ApiResponseDto<IEnumerable<CardDto>>.Ok(list));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<CardDto>>>> Search(Guid boardId, [FromQuery] string q, CancellationToken ct)
    {
        var list = await _cards.SearchAsync(boardId, q, ct);
        return Ok(ApiResponseDto<IEnumerable<CardDto>>.Ok(list));
    }

    [HttpPut("{cardId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Update(Guid cardId, [FromBody] CardUpdateDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();

        await _cards.UpdateAsync(cardId, userId, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    [HttpDelete("{cardId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid cardId, CancellationToken ct)
    {
        await _cards.DeleteAsync(cardId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }
    
    // CardRead endpoints
    [HttpPost("{cardId:guid}/read")]
    public async Task<ActionResult<ApiResponseDto<object>>> MarkRead(Guid cardId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _cards.MarkAsReadAsync(cardId, userId, ct);

        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    [HttpGet("{cardId:guid}/read")]
    public async Task<ActionResult<ApiResponseDto<DateTime?>>> GetLastRead(Guid cardId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var date = await _cards.GetLastReadAsync(cardId, userId, ct);

        return Ok(ApiResponseDto<DateTime?>.Ok(date));
    }
}