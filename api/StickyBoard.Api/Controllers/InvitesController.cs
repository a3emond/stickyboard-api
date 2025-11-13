using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Services.SocialAndMessaging;

namespace StickyBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class InvitesController : ControllerBase
{
    private readonly InviteService _invites;

    public InvitesController(InviteService invites)
    {
        _invites = invites;
    }

    // ------------------------------------------------------------
    // CREATE INVITE
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<InviteCreateResponseDto>>> Create(
        [FromBody] InviteCreateRequestDto dto,
        CancellationToken ct)
    {
        var senderId = User.GetUserId();
        if (senderId == Guid.Empty)
            return Unauthorized(ApiResponseDto<InviteCreateResponseDto>.Fail("Invalid or missing token."));

        dto.SenderId = senderId;

        var result = await _invites.CreateAsync(dto, ct);
        return Ok(ApiResponseDto<InviteCreateResponseDto>.Ok(result));
    }

    // ------------------------------------------------------------
    // ACCEPT INVITE
    // ------------------------------------------------------------
    [HttpPost("accept")]
    public async Task<ActionResult<ApiResponseDto<InviteAcceptResponseDto>>> Accept(
        [FromBody] InviteAcceptRequestDto dto,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<InviteAcceptResponseDto>.Fail("Invalid or missing token."));

        dto.AcceptingUserId = userId;

        var result = await _invites.AcceptAsync(dto, ct);
        return result is null
            ? NotFound(ApiResponseDto<InviteAcceptResponseDto>.Fail("Invalid or expired invite."))
            : Ok(ApiResponseDto<InviteAcceptResponseDto>.Ok(result));
    }

    // ------------------------------------------------------------
    // REVOKE INVITE
    // ------------------------------------------------------------
    [HttpPost("revoke")]
    public async Task<ActionResult<ApiResponseDto<object>>> Revoke(
        [FromBody] InviteRevokeRequestDto dto,
        CancellationToken ct)
    {
        var senderId = User.GetUserId();
        if (senderId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        var ok = await _invites.RevokeAsync(dto.Token, ct);

        return ok
            ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
            : NotFound(ApiResponseDto<object>.Fail("Invite not found or already handled."));
    }

    // ------------------------------------------------------------
    // SENT INVITES
    // ------------------------------------------------------------
    [HttpGet("sent")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<InviteDto>>>> GetSent(
        CancellationToken ct)
    {
        var senderId = User.GetUserId();
        if (senderId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<InviteDto>>.Fail("Invalid or missing token."));

        var invites = await _invites.GetBySenderAsync(senderId, ct);
        return Ok(ApiResponseDto<IEnumerable<InviteDto>>.Ok(invites));
    }

    // ------------------------------------------------------------
    // LOOKUP BY EMAIL
    // ------------------------------------------------------------
    [HttpGet("email")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<InviteDto>>>> GetByEmail(
        [FromQuery] string email,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(ApiResponseDto<IEnumerable<InviteDto>>.Fail("Email is required."));

        var invites = await _invites.GetByEmailAsync(email, ct);
        return Ok(ApiResponseDto<IEnumerable<InviteDto>>.Ok(invites));
    }

    // ------------------------------------------------------------
    // PUBLIC LOOKUP (NO AUTH)
    // ------------------------------------------------------------
    [AllowAnonymous]
    [HttpGet("public/{token}")]
    public async Task<ActionResult<ApiResponseDto<InviteDto>>> GetPublic(
        string token,
        CancellationToken ct)
    {
        var invite = await _invites.GetByTokenAsync(token, ct);

        return invite is null
            ? NotFound(ApiResponseDto<InviteDto>.Fail("Invite not found."))
            : Ok(ApiResponseDto<InviteDto>.Ok(invite));
    }
}
