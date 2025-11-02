using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/folders")]
public class BoardFolderController : ControllerBase
{
    private readonly BoardFolderService _service;

    public BoardFolderController(BoardFolderService service)
    {
        _service = service;
    }

    private Guid UserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _service.GetAccessibleAsync(UserId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(BoardFolderCreateDto dto, CancellationToken ct)
    {
        var id = await _service.CreateAsync(UserId, dto, ct);
        return Ok(new { Id = id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, BoardFolderUpdateDto dto, CancellationToken ct)
        => Ok(await _service.UpdateAsync(UserId, id, dto, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Ok(await _service.DeleteAsync(UserId, id, ct));
}