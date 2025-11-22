using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Attachments;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Services.Attachments;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AttachmentsController : ControllerBase
{
    private readonly AttachmentService _attachments;

    public AttachmentsController(AttachmentService attachments)
    {
        _attachments = attachments;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<AttachmentDto>>> Create(
        [FromBody] AttachmentCreateDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<AttachmentDto>.Fail("Invalid or missing token."));

        var a = await _attachments.CreateAsync(userId, dto, ct);
        return Ok(ApiResponseDto<AttachmentDto>.Ok(a));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Update(
        Guid id,
        [FromBody] AttachmentUpdateDto dto,
        CancellationToken ct)
    {
        var ok = await _attachments.UpdateAsync(id, dto, ct);
        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Attachment not found or version mismatch."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _attachments.DeleteAsync(id, ct);
        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Attachment not found."));
    }

    [HttpGet("card/{cardId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<AttachmentDto>>>> GetForCard(
        Guid cardId,
        CancellationToken ct)
    {
        var list = await _attachments.GetForCardAsync(cardId, ct);
        return Ok(ApiResponseDto<IEnumerable<AttachmentDto>>.Ok(list));
    }

    [HttpGet("board/{boardId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<AttachmentDto>>>> GetForBoard(
        Guid boardId,
        CancellationToken ct)
    {
        var list = await _attachments.GetForBoardAsync(boardId, ct);
        return Ok(ApiResponseDto<IEnumerable<AttachmentDto>>.Ok(list));
    }

    [HttpGet("workspace/{workspaceId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<AttachmentDto>>>> GetForWorkspace(
        Guid workspaceId,
        CancellationToken ct)
    {
        var list = await _attachments.GetForWorkspaceAsync(workspaceId, ct);
        return Ok(ApiResponseDto<IEnumerable<AttachmentDto>>.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<AttachmentDto>>> Get(Guid id, CancellationToken ct)
    {
        var a = await _attachments.GetAsync(id, ct);
        return a is not null
            ? Ok(ApiResponseDto<AttachmentDto>.Ok(a))
            : NotFound(ApiResponseDto<AttachmentDto>.Fail("Attachment not found."));
    }
}
