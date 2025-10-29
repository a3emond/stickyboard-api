using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // Enqueue new job manually or via trigger
        [HttpPost("{kind}")]
        public async Task<IActionResult> Enqueue(JobKind kind, [FromBody] object payload, CancellationToken ct)
        {
            var id = await _jobs.EnqueueAsync(kind, payload, ct: ct);
            return Ok(new { Id = id });
        }

        // List queued jobs
        [HttpGet("queued")]
        public async Task<IActionResult> GetQueued(CancellationToken ct)
        {
            var list = await _jobs.GetQueuedAsync(ct);
            return Ok(list);
        }

        // List job attempts
        [HttpGet("{jobId:guid}/attempts")]
        public async Task<IActionResult> GetAttempts(Guid jobId, CancellationToken ct)
        {
            var list = await _jobs.GetAttemptsAsync(jobId, ct);
            return Ok(list);
        }

        // List all deadletters
        [HttpGet("deadletters")]
        public async Task<IActionResult> GetDeadletters(CancellationToken ct)
        {
            var list = await _jobs.GetDeadlettersAsync(ct);
            return Ok(list);
        }

        // Cleanup old jobs
        [HttpDelete("cleanup")]
        public async Task<IActionResult> Cleanup([FromQuery] int retentionDays = 30, CancellationToken ct = default)
        {
            var count = await _jobs.CleanupAsync(TimeSpan.FromDays(retentionDays), ct);
            return Ok(new { Deleted = count });
        }
    }
}