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
public sealed class FileTokensController : ControllerBase
{
    private readonly FileTokenService _tokens;

    public FileTokensController(FileTokenService tokens)
    {
        _tokens = tokens;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<FileTokenDto>>> Create(
        [FromBody] FileTokenCreateDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<FileTokenDto>.Fail("Invalid or missing token."));

        var t = await _tokens.CreateAsync(userId, dto, ct);
        return Ok(ApiResponseDto<FileTokenDto>.Ok(t));
    }

    [HttpGet("attachment/{attachmentId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<FileTokenDto>>>> GetValidForAttachment(
        Guid attachmentId,
        CancellationToken ct)
    {
        var list = await _tokens.GetValidForAttachmentAsync(attachmentId, ct);
        return Ok(ApiResponseDto<IEnumerable<FileTokenDto>>.Ok(list));
    }

    [HttpPut("{id:guid}/revoke")]
    public async Task<ActionResult<ApiResponseDto<object>>> Revoke(Guid id, CancellationToken ct)
    {
        var ok = await _tokens.RevokeAsync(id, ct);
        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Token not found or already revoked."));
    }

    [HttpPut("attachment/{attachmentId:guid}/revoke-all")]
    public async Task<ActionResult<ApiResponseDto<object>>> RevokeAll(Guid attachmentId, CancellationToken ct)
    {
        var count = await _tokens.RevokeAllForAttachmentAsync(attachmentId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true, updated = count }));
    }
}
