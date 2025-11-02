using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Services;

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
    // CREATE
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<InviteCreateResponseDto>>> Create([FromBody] InviteCreateDto dto, CancellationToken ct)
    {
        var senderId = User.GetUserId();
        if (senderId == Guid.Empty)
            return Unauthorized(ApiResponseDto<InviteCreateResponseDto>.Fail("Invalid or missing token."));

        var result = await _invites.CreateAsync(senderId, dto, ct);
        return Ok(ApiResponseDto<InviteCreateResponseDto>.Ok(result));
    }

    // ------------------------------------------------------------
    // REDEEM
    // ------------------------------------------------------------
    [HttpPost("redeem")]
    public async Task<ActionResult<ApiResponseDto<object>>> Redeem([FromBody] InviteRedeemRequestDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        await _invites.RedeemAsync(userId, dto.Token, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // RECEIVED INVITES
    // ------------------------------------------------------------
    [HttpGet("received")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<InviteListItemDto>>>> GetReceived(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<InviteListItemDto>>.Fail("Invalid or missing token."));

        var list = await _invites.GetPendingForUserAsync(userId, ct);
        return Ok(ApiResponseDto<IEnumerable<InviteListItemDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // SENT INVITES
    // ------------------------------------------------------------
    [HttpGet("sent")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<InviteListItemDto>>>> GetSent(CancellationToken ct)
    {
        var senderId = User.GetUserId();
        if (senderId == Guid.Empty)
            return Unauthorized(ApiResponseDto<IEnumerable<InviteListItemDto>>.Fail("Invalid or missing token."));

        var list = await _invites.GetPendingSentAsync(senderId, ct);
        return Ok(ApiResponseDto<IEnumerable<InviteListItemDto>>.Ok(list));
    }

    // ------------------------------------------------------------
    // CANCEL
    // ------------------------------------------------------------
    [HttpDelete("{inviteId:guid}")]
    public async Task<ActionResult<ApiResponseDto<object>>> Cancel(Guid inviteId, CancellationToken ct)
    {
        var senderId = User.GetUserId();
        if (senderId == Guid.Empty)
            return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

        await _invites.CancelAsync(senderId, inviteId, ct);
        return Ok(ApiResponseDto<object>.Ok(new { success = true }));
    }

    // ------------------------------------------------------------
    // PUBLIC LOOKUP
    // ------------------------------------------------------------
    [AllowAnonymous]
    [HttpGet("public/{token}")]
    public async Task<ActionResult<ApiResponseDto<InvitePublicDto>>> GetPublic(string token, CancellationToken ct)
    {
        var result = await _invites.GetPublicByTokenAsync(token, ct);
        return result is null
            ? NotFound(ApiResponseDto<InvitePublicDto>.Fail("Invite not found."))
            : Ok(ApiResponseDto<InvitePublicDto>.Ok(result));
    }
}
