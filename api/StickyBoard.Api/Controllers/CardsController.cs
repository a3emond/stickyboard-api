using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public sealed class CardsController : ControllerBase
{
    private readonly CardService _service;

    public CardsController(CardService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var user = User.GetUserId();
        return Ok(ApiResponseDto<CardDto>.Ok(await _service.GetAsync(user, id, ct)));
    }

    [HttpGet("tab/{tabId:guid}")]
    public async Task<IActionResult> GetByTab(Guid tabId, CancellationToken ct)
    {
        var user = User.GetUserId();
        return Ok(ApiResponseDto<IEnumerable<CardDto>>.Ok(await _service.GetByTabAsync(user, tabId, ct)));
    }

    [HttpGet("section/{sectionId:guid}")]
    public async Task<IActionResult> GetBySection(Guid sectionId, CancellationToken ct)
    {
        var user = User.GetUserId();
        return Ok(ApiResponseDto<IEnumerable<CardDto>>.Ok(await _service.GetBySectionAsync(user, sectionId, ct)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CardCreateDto dto, CancellationToken ct)
    {
        var user = User.GetUserId();
        var id = await _service.CreateAsync(user, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { id }));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CardUpdateDto dto, CancellationToken ct)
    {
        var user = User.GetUserId();
        await _service.UpdateAsync(user, id, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var user = User.GetUserId();
        await _service.DeleteAsync(user, id, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }
}
