using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Sync;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class SyncController : ControllerBase
    {
        private readonly SyncService _sync;

        public SyncController(SyncService sync)
        {
            _sync = sync;
        }

        // ------------------------------------------------------------
        // POST /api/sync/commit
        // ------------------------------------------------------------
        [HttpPost("commit")]
        public async Task<IActionResult> Commit([FromBody] SyncCommitRequestDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _sync.CommitAsync(userId, dto, ct);
            return Ok(result);
        }

        // ------------------------------------------------------------
        // GET /api/sync/pull?since=...
        // ------------------------------------------------------------
        [HttpGet("pull")]
        public async Task<IActionResult> Pull([FromQuery] DateTime since, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _sync.PullAsync(userId, since, ct);
            return Ok(result);
        }
    }
}