using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/boards/{boardId:guid}/messages")]
public class BoardMessageController : ControllerBase
{
    private readonly BoardMessageService _service;
    public BoardMessageController(BoardMessageService service) => _service = service;

    private Guid UserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<IActionResult> Get(Guid boardId, CancellationToken ct)
        => Ok(await _service.GetForBoardAsync(UserId, boardId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(Guid boardId, BoardMessageCreateDto dto, CancellationToken ct)
    {
        var id = await _service.CreateAsync(UserId, boardId, dto, ct);
        return Ok(new { Id = id });
    }
}