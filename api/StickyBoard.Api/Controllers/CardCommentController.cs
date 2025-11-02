using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/cards/{cardId:guid}/comments")]
public class CardCommentController : ControllerBase
{
    private readonly CardCommentService _service;
    public CardCommentController(CardCommentService service) => _service = service;

    private Guid UserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<IActionResult> Get(Guid cardId, CancellationToken ct)
        => Ok(await _service.GetForCardAsync(UserId, cardId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(Guid cardId, CardCommentCreateDto dto, CancellationToken ct)
    {
        var id = await _service.CreateAsync(UserId, cardId, dto, ct);
        return Ok(new { Id = id });
    }
}