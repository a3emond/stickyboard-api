using System.Text.Json;
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
    private readonly AttachmentService _attachments;

    public AttachmentsController(AttachmentService attachments)
    {
        _attachments = attachments;
    }

    // ------------------------------------------------------------
    // CRUD
    // ------------------------------------------------------------

    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<AttachmentDto>>> Create(
        [FromBody] AttachmentCreateDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<AttachmentDto>.Fail("Invalid token"));

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
            : NotFound(ApiResponseDto<object>.Fail("Attachment not found or version mismatch"));
    }

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

    // ------------------------------------------------------------
    // QUERIES
    // ------------------------------------------------------------

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
    public async Task<ActionResult<ApiResponseDto<AttachmentDto>>> Get(
        Guid id,
        CancellationToken ct)
    {
        var a = await _attachments.GetAsync(id, ct);

        return a is not null
            ? Ok(ApiResponseDto<AttachmentDto>.Ok(a))
            : NotFound(ApiResponseDto<AttachmentDto>.Fail("Attachment not found"));
    }

    // ------------------------------------------------------------
    // UPLOAD (CDN PIPELINE)
    // ------------------------------------------------------------

    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<ApiResponseDto<AttachmentDto>>> Upload(
        [FromForm] IFormFile file,
        [FromForm] Guid boardId,
        [FromForm] Guid? cardId,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<AttachmentDto>.Fail("Invalid token"));

        if (file is null || file.Length == 0)
            return BadRequest(ApiResponseDto<AttachmentDto>.Fail("Empty file"));

        // Prevent traversal
        if (Path.GetFileName(file.FileName) != file.FileName)
            return BadRequest(ApiResponseDto<AttachmentDto>.Fail("Invalid filename"));

        var attachmentId = Guid.NewGuid();
        var safeName = file.FileName;

        var storagePath = $"boards/{boardId}/att/{attachmentId}/original/{safeName}";

        // 1. Create attachment in DB (UPLOADING)
        var createDto = new AttachmentCreateDto
        {
            WorkspaceId = null,
            BoardId = boardId,
            CardId = cardId,
            Filename = safeName,
            Mime = file.ContentType,
            ByteSize = file.Length,
            StoragePath = storagePath,
            IsPublic = false,
            Status = AttachmentStatus.Uploading,
            Meta = JsonDocument.Parse("{}")
        };

        var attachment = await _attachments.CreateAsync(userId, createDto, ct);

        // 2. Generate signed CDN upload URL
        var uploadUrl = await _attachments.GenerateUploadUrlAsync(attachment.Id, ct);

        // 3. Stream file to CDN
        using var http = new HttpClient();
        using var stream = file.OpenReadStream();

        var content = new StreamContent(stream);
        content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

        var response = await http.PutAsync(uploadUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            await _attachments.FailAsync(attachment.Id, ct);
            return StatusCode(502, ApiResponseDto<AttachmentDto>.Fail("CDN upload failed"));
        }

        // 4. Mark READY
        var completed = await _attachments.MarkReadyAsync(attachment.Id, ct);

        return Ok(ApiResponseDto<AttachmentDto>.Ok(completed));
    }
}
