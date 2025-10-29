using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Operations;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public sealed class OperationsController : ControllerBase
    {
        private readonly OperationService _operations;

        public OperationsController(OperationService operations)
        {
            _operations = operations;
        }

        // ------------------------------------------------------------
        // APPEND (log or sync operation)
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Append([FromBody] CreateOperationDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var id = await _operations.AppendAsync(userId, dto, ct);
            return Ok(new { Id = id });
        }

        // ------------------------------------------------------------
        // QUERY (get operations by device, user, or since timestamp)
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Query([FromQuery] OperationQueryDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var list = await _operations.QueryAsync(userId, dto, ct);
            return Ok(list);
        }

        // ------------------------------------------------------------
        // MAINTENANCE (admin/worker cleanup endpoint)
        // ------------------------------------------------------------
        [Authorize(Roles = "admin")]
        [HttpPost("maintenance")]
        public async Task<IActionResult> Maintenance([FromQuery] int retentionDays = 30, CancellationToken ct = default)
        {
            var result = await _operations.MaintenanceAsync(TimeSpan.FromDays(retentionDays), ct);
            return Ok(result);
        }
    }
}
