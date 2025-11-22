using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.DTOs.Attachments;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.Services.Attachments;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/attachments/{attachmentId:guid}/variants")]
[Authorize]
public sealed class AttachmentVariantsController : ControllerBase
{
    private readonly AttachmentVariantService _variants;

    public AttachmentVariantsController(AttachmentVariantService variants)
    {
        _variants = variants;
    }

    // GET /api/attachments/{id}/variants
    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<AttachmentVariantDto>>>> GetAll(
        Guid attachmentId,
        CancellationToken ct)
    {
        var list = await _variants.GetForParentAsync(attachmentId, ct);
        return Ok(ApiResponseDto<IEnumerable<AttachmentVariantDto>>.Ok(list));
    }

    // GET /api/attachments/{id}/variants/{variant}
    [HttpGet("{variant}")]
    public async Task<ActionResult<ApiResponseDto<AttachmentVariantDto>>> GetOne(
        Guid attachmentId,
        string variant,
        CancellationToken ct)
    {
        var v = await _variants.GetAsync(attachmentId, variant, ct);

        return v is not null
            ? Ok(ApiResponseDto<AttachmentVariantDto>.Ok(v))
            : NotFound(ApiResponseDto<AttachmentVariantDto>.Fail("Variant not found."));
    }
}