using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.Common;
using StickyBoard.Api.DTOs.Worker;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Services;

namespace StickyBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",ApiKey", Roles = "admin,worker")]
    public sealed class WorkerJobsController : ControllerBase
    {
        private readonly WorkerJobService _jobs;

        public WorkerJobsController(WorkerJobService jobs)
        {
            _jobs = jobs;
        }

        // ------------------------------------------------------------
        // POST /api/workerjobs
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<object>>> Enqueue([FromBody] EnqueueWorkerJobDto dto, CancellationToken ct)
        {
            var id = await _jobs.EnqueueAsync(dto.Kind, dto.Payload, dto.Priority, dto.MaxAttempts, ct);
            return Ok(ApiResponseDto<object>.Ok(new { id }));
        }

        // ------------------------------------------------------------
        // GET /api/workerjobs/queued
        // ------------------------------------------------------------
        [HttpGet("queued")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<WorkerJobDto>>>> GetQueued(CancellationToken ct)
        {
            var list = await _jobs.GetQueuedAsync(ct);
            return Ok(ApiResponseDto<IEnumerable<WorkerJobDto>>.Ok(list));
        }

        // ------------------------------------------------------------
        // GET /api/workerjobs/{jobId}/attempts
        // ------------------------------------------------------------
        [HttpGet("{jobId:guid}/attempts")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<WorkerJobAttemptDto>>>> GetAttempts(Guid jobId, CancellationToken ct)
        {
            var list = await _jobs.GetAttemptsAsync(jobId, ct);
            return Ok(ApiResponseDto<IEnumerable<WorkerJobAttemptDto>>.Ok(list));
        }

        // ------------------------------------------------------------
        // GET /api/workerjobs/deadletters
        // ------------------------------------------------------------
        [HttpGet("deadletters")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<WorkerJobDeadletterDto>>>> GetDeadletters(CancellationToken ct)
        {
            var list = await _jobs.GetDeadlettersAsync(ct);
            return Ok(ApiResponseDto<IEnumerable<WorkerJobDeadletterDto>>.Ok(list));
        }

        // ------------------------------------------------------------
        // DELETE /api/workerjobs/cleanup?retentionDays=30
        // ------------------------------------------------------------
        [HttpDelete("cleanup")]
        public async Task<ActionResult<ApiResponseDto<object>>> Cleanup([FromQuery] int retentionDays = 30, CancellationToken ct = default)
        {
            var count = await _jobs.CleanupAsync(TimeSpan.FromDays(retentionDays), ct);
            return Ok(ApiResponseDto<object>.Ok(new { deleted = count }));
        }
    }
}
