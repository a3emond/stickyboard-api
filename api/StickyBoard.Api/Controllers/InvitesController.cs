using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Messaging;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // all except /public require JWT
    public sealed class InvitesController : ControllerBase
    {
        private readonly InviteService _invites;

        public InvitesController(InviteService invites) => _invites = invites;

        // ------------------------------------------------------------
        // AUTH: Create invite
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InviteCreateDto dto, CancellationToken ct)
        {
            var senderId = User.GetUserId();
            if (senderId == Guid.Empty) return Unauthorized();

            var res = await _invites.CreateAsync(senderId, dto, ct);
            return Ok(res);
        }

        // ------------------------------------------------------------
        // AUTH: Redeem invite
        // ------------------------------------------------------------
        [HttpPost("redeem")]
        public async Task<IActionResult> Redeem([FromBody] InviteRedeemRequestDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var ok = await _invites.RedeemAsync(userId, dto.Token, ct);
            return ok ? Ok(new { success = true }) : BadRequest();
        }

        // ------------------------------------------------------------
        // AUTH: List invites addressed to me
        // ------------------------------------------------------------
        [HttpGet("received")]
        public async Task<IActionResult> GetMyPending(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var list = await _invites.GetPendingForUserAsync(userId, ct);
            return Ok(list);
        }

        // ------------------------------------------------------------
        // AUTH: List invites I sent
        // ------------------------------------------------------------
        [HttpGet("sent")]
        public async Task<IActionResult> GetSent(CancellationToken ct)
        {
            var senderId = User.GetUserId();
            if (senderId == Guid.Empty) return Unauthorized();

            var list = await _invites.GetPendingSentAsync(senderId, ct);
            return Ok(list);
        }

        // ------------------------------------------------------------
        // AUTH: Cancel invite I sent (if still pending)
        // ------------------------------------------------------------
        [HttpDelete("{inviteId:guid}")]
        public async Task<IActionResult> Cancel(Guid inviteId, CancellationToken ct)
        {
            var senderId = User.GetUserId();
            if (senderId == Guid.Empty) return Unauthorized();

            var ok = await _invites.CancelAsync(senderId, inviteId, ct);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        // ------------------------------------------------------------
        // PUBLIC: Lookup invite by token (for landing page)
        // ------------------------------------------------------------
        [AllowAnonymous]
        [HttpGet("public/{token}")]
        public async Task<IActionResult> GetPublic(string token, CancellationToken ct)
        {
            var invite = await _invites.GetPublicByTokenAsync(token, ct);
            if (invite is null) return NotFound();
            return Ok(invite);
        }
    }
}
