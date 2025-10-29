using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Messaging;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
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
        // AUTH: Create invite
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<InviteCreateResponseDto>>> Create([FromBody] InviteCreateDto dto, CancellationToken ct)
        {
            var senderId = User.GetUserId();
            if (senderId == Guid.Empty)
                return Unauthorized(ApiResponseDto<InviteCreateResponseDto>.Fail("Invalid or missing token."));

            var res = await _invites.CreateAsync(senderId, dto, ct);
            return Ok(ApiResponseDto<InviteCreateResponseDto>.Ok(res));
        }

        // ------------------------------------------------------------
        // AUTH: Redeem invite
        // ------------------------------------------------------------
        [HttpPost("redeem")]
        public async Task<ActionResult<ApiResponseDto<object>>> Redeem([FromBody] InviteRedeemRequestDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _invites.RedeemAsync(userId, dto.Token, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : BadRequest(ApiResponseDto<object>.Fail("Failed to redeem invite."));
        }

        // ------------------------------------------------------------
        // AUTH: List invites addressed to me
        // ------------------------------------------------------------
        [HttpGet("received")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<InviteListItemDto>>>> GetMyPending(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<InviteListItemDto>>.Fail("Invalid or missing token."));

            var list = await _invites.GetPendingForUserAsync(userId, ct);
            return Ok(ApiResponseDto<IEnumerable<InviteListItemDto>>.Ok(list));
        }

        // ------------------------------------------------------------
        // AUTH: List invites I sent
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
        // AUTH: Cancel invite I sent (if still pending)
        // ------------------------------------------------------------
        [HttpDelete("{inviteId:guid}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Cancel(Guid inviteId, CancellationToken ct)
        {
            var senderId = User.GetUserId();
            if (senderId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var ok = await _invites.CancelAsync(senderId, inviteId, ct);
            return ok
                ? Ok(ApiResponseDto<object>.Ok(new { success = true }))
                : NotFound(ApiResponseDto<object>.Fail("Invite not found."));
        }

        // ------------------------------------------------------------
        // PUBLIC: Lookup invite by token (for landing page)
        // ------------------------------------------------------------
        [AllowAnonymous]
        [HttpGet("public/{token}")]
        public async Task<ActionResult<ApiResponseDto<InvitePublicDto>>> GetPublic(string token, CancellationToken ct)
        {
            var invite = await _invites.GetPublicByTokenAsync(token, ct);
            if (invite is null)
                return NotFound(ApiResponseDto<InvitePublicDto>.Fail("Invite not found."));

            return Ok(ApiResponseDto<InvitePublicDto>.Ok(invite));
        }
    }
}
