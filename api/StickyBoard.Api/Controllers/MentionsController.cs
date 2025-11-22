using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models;
using StickyBoard.Api.Services.SocialAndMessaging;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MentionsController : ControllerBase
{
    private readonly MentionService _mentions;

    public MentionsController(MentionService mentions)
    {
        _mentions = mentions;
    }

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<MentionDto>>> Create(
        [FromBody] MentionCreateDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<MentionDto>.Fail("Invalid or missing token."));

        var m = await _mentions.CreateAsync(userId, dto, ct);
        return Ok(ApiResponseDto<MentionDto>.Ok(m));
    }

    // ------------------------------------------------------------
    // GET MY MENTIONS
    // ------------------------------------------------------------
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<MentionDto>>>> GetMyMentions(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<MentionDto>>.Fail("Invalid or missing token."));

        var list = await _mentions.GetForUserAsync(userId, ct);
        return Ok(ApiResponseDto<IEnumerable<MentionDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // GET BY ENTITY (debug / admin / tooling)
    // ------------------------------------------------------------
    [HttpGet("entity")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<MentionDto>>>> GetByEntity(
        [FromQuery] EntityType type,
        [FromQuery] Guid entityId,
        CancellationToken ct)
    {
        var list = await _mentions.GetForEntityAsync(type, entityId, ct);
        return Ok(ApiResponseDto<IEnumerable<MentionDto>>.Ok(list));
    }
}
