using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Operations;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class OperationsController : ControllerBase
    {
        private readonly OperationService _operations;

        public OperationsController(OperationService operations)
        {
            _operations = operations;
        }

        // ------------------------------------------------------------
        // APPEND
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> Append([FromBody] CreateOperationDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<object>.Fail("Invalid or missing token."));

            var id = await _operations.AppendAsync(userId, dto, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        // ------------------------------------------------------------
        // QUERY
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<OperationDto>>>> Query([FromQuery] OperationQueryDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponseDto<IEnumerable<OperationDto>>.Fail("Invalid or missing token."));

            var list = await _operations.QueryAsync(userId, dto, ct);
            return Ok(ApiResponseDto<IEnumerable<OperationDto>>.Ok(list));
        }

        // ------------------------------------------------------------
        // MAINTENANCE
        // ------------------------------------------------------------
        [Authorize(Roles = "admin")]
        [HttpPost("maintenance")]
        public async Task<ActionResult<ApiResponseDto<OperationMaintenanceResultDto>>> Maintenance(
            [FromQuery] int retentionDays = 30,
            CancellationToken ct = default)
        {
            var result = await _operations.MaintenanceAsync(TimeSpan.FromDays(retentionDays), ct);
            return Ok(ApiResponseDto<OperationMaintenanceResultDto>.Ok(result));
        }
    }
}
