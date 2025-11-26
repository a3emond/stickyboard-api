using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Core.DTOs.Attachments;
using StickyBoard.Core.DTOs.Common;
using StickyBoard.Core.Models;
using StickyBoard.Core.Services.Attachments;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AttachmentsController : ControllerBase
{
    private readonly IAttachmentManager _attachments;
    private readonly HttpClient _http;

    public AttachmentsController(IAttachmentManager attachments, HttpClient http)
    {
        _attachments = attachments;
        _http = http;
    }


    // ------------------------------------------------------------
    // METADATA QUERIES
    // ------------------------------------------------------------

    [HttpGet("card/{cardId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<AttachmentMetaDto>>>> GetForCard(
        Guid cardId,
        CancellationToken ct)
    {
        var list = await _attachments.GetForCardAsync(cardId, ct);
        return Ok(ApiResponseDto<IEnumerable<AttachmentMetaDto>>.Ok(list));
    }

    [HttpGet("board/{boardId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<AttachmentMetaDto>>>> GetForBoard(
        Guid boardId,
        CancellationToken ct)
    {
        var list = await _attachments.GetForBoardAsync(boardId, ct);
        return Ok(ApiResponseDto<IEnumerable<AttachmentMetaDto>>.Ok(list));
    }

    [HttpGet("workspace/{workspaceId:guid}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<AttachmentMetaDto>>>> GetForWorkspace(
        Guid workspaceId,
        CancellationToken ct)
    {
        var list = await _attachments.GetForWorkspaceAsync(workspaceId, ct);
        return Ok(ApiResponseDto<IEnumerable<AttachmentMetaDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // DOWNLOAD URL
    // ------------------------------------------------------------

    [HttpGet("{id:guid}/download-url")]
    public async Task<ActionResult<ApiResponseDto<AttachmentDownloadResult>>> GetDownloadUrl(
        Guid id,
        [FromQuery] string? variant,
        CancellationToken ct)
    {
        var result = await _attachments.GetDownloadUrlAsync(id, variant, ct);
        return Ok(ApiResponseDto<AttachmentDownloadResult>.Ok(result));
    }

// ------------------------------------------------------------
// UPLOAD (CDN PIPELINE)
// ------------------------------------------------------------

    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponseDto<AttachmentMetaDto>>> Upload(
        IFormFile file,
        [FromForm] Guid boardId,
        [FromForm] Guid? cardId,
        CancellationToken ct)

{
    var userId = User.GetUserId();
    if (userId == Guid.Empty)
        return Unauthorized(ApiResponseDto<AttachmentMetaDto>.Fail("Invalid token"));

    if (file is null || file.Length == 0)
        return BadRequest(ApiResponseDto<AttachmentMetaDto>.Fail("Empty file"));

    // Prevent path traversal
    if (Path.GetFileName(file.FileName) != file.FileName)
        return BadRequest(ApiResponseDto<AttachmentMetaDto>.Fail("Invalid filename"));

    var safeName = file.FileName;

    // 1. Init attachment + get signed upload URL
    var initRequest = new AttachmentInitRequest
    {
        WorkspaceId = null,
        BoardId     = boardId,
        CardId      = cardId,
        Filename    = safeName,
        Mime        = file.ContentType,
        ByteSize    = file.Length,
        IsPublic    = false
    };

    var init = await _attachments.InitUploadAsync(userId, initRequest, ct);

    // 2. Stream file to CDN using injected HttpClient
    await using var stream = file.OpenReadStream();

    var content = new StreamContent(stream);
    content.Headers.ContentType =
        new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

    var response = await _http.PutAsync(init.UploadUrl, content, ct);

    if (!response.IsSuccessStatusCode)
    {
        await _attachments.MarkUploadFailedAsync(init.AttachmentId, ct);
        return StatusCode(502, ApiResponseDto<AttachmentMetaDto>.Fail("CDN upload failed"));
    }

    // 3. Mark READY + enqueue variant worker
    await _attachments.MarkUploadCompleteAsync(init.AttachmentId, ct);

    // 4. Return minimal metadata for the newly created attachment
    var dto = new AttachmentMetaDto
    {
        Id       = init.AttachmentId,
        Filename = initRequest.Filename,
        Mime     = initRequest.Mime,
        ByteSize = initRequest.ByteSize,
        Status   = AttachmentStatus.Ready,
        Variants = Array.Empty<VariantMetaDto>()
    };

    return Ok(ApiResponseDto<AttachmentMetaDto>.Ok(dto));
}

    // ------------------------------------------------------------
    // DELETE + TOKEN CLEANUP
    // ------------------------------------------------------------

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Delete(
        Guid id,
        CancellationToken ct)
    {
        var ok = await _attachments.DeleteAsync(id, ct);

        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Attachment not found"));
    }

    [HttpPost("{id:guid}/revoke-tokens")]
    public async Task<ActionResult<ApiResponseDto<object>>> RevokeTokens(
        Guid id,
        CancellationToken ct)
    {
        await _attachments.RevokeAllTokensAsync(id, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }
}
