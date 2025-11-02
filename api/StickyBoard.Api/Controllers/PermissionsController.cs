using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/boards/{boardId:guid}/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly PermissionService _service;
    public PermissionsController(PermissionService service) => _service = service;

    private Guid UserId() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Get(Guid boardId, CancellationToken ct)
    {
        var data = await _service.GetByBoardAsync(UserId(), boardId, ct);
        return Ok(ApiResponseDto<IEnumerable<PermissionDto>>.Ok(data));
    }

    [HttpGet("~/api/users/{userId:guid}/boards")]
    public async Task<IActionResult> GetUserBoards(Guid userId, CancellationToken ct)
    {
        var data = await _service.GetByUserAsync(UserId(), userId, ct);
        return Ok(ApiResponseDto<IEnumerable<PermissionDto>>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid boardId, [FromBody] GrantPermissionDto dto, CancellationToken ct)
    {
        var id = await _service.AddAsync(UserId(), boardId, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { id }));
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> Update(Guid boardId, Guid userId, [FromBody] UpdatePermissionDto dto, CancellationToken ct)
    {
        await _service.UpdateAsync(UserId(), boardId, userId, dto, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid boardId, Guid userId, CancellationToken ct)
    {
        await _service.RemoveAsync(UserId(), boardId, userId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }
}