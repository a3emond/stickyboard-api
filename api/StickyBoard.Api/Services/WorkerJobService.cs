using StickyBoard.Api.DTOs.Worker;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Models.Worker;
using StickyBoard.Api.Repositories;
using System.Text.Json;

namespace StickyBoard.Api.Services
{
    public sealed class WorkerJobService
    {
        private readonly WorkerJobRepository _jobs;
        private readonly WorkerJobAttemptRepository _attempts;
        private readonly WorkerJobDeadletterRepository _deadletters;

        public WorkerJobService(
            WorkerJobRepository jobs,
            WorkerJobAttemptRepository attempts,
            WorkerJobDeadletterRepository deadletters)
        {
            _jobs = jobs;
            _attempts = attempts;
            _deadletters = deadletters;
        }

        // ------------------------------------------------------------
        // ENQUEUE NEW JOB
        // ------------------------------------------------------------
        public async Task<Guid> EnqueueAsync(
            JobKind kind,
            object payload,
            short priority = 0,
            short maxAttempts = 3,
            CancellationToken ct = default)
        {
            var job = new WorkerJob
            {
                Id = Guid.NewGuid(),
                JobKind = kind,
                Priority = priority,
                RunAt = DateTime.UtcNow,
                MaxAttempts = maxAttempts,
                Attempt = 0,
                Payload = JsonDocument.Parse(JsonSerializer.Serialize(payload)),
                Status = JobStatus.queued,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _jobs.CreateAsync(job, ct);
        }

        // ------------------------------------------------------------
        // ADMIN / MONITORING
        // ------------------------------------------------------------

        public async Task<IEnumerable<WorkerJobDto>> GetQueuedAsync(CancellationToken ct)
        {
            var jobs = await _jobs.GetQueuedAsync(ct);
            return jobs.Select(j => new WorkerJobDto
            {
                Id = j.Id,
                JobKind = j.JobKind,
                Priority = j.Priority,
                RunAt = j.RunAt,
                MaxAttempts = j.MaxAttempts,
                Attempt = (short)j.Attempt,
                Status = j.Status,
                PayloadJson = j.Payload?.RootElement.GetRawText(),
                CreatedAt = j.CreatedAt,
                UpdatedAt = j.UpdatedAt
            });
        }

        public async Task<IEnumerable<WorkerJobAttemptDto>> GetAttemptsAsync(Guid jobId, CancellationToken ct)
        {
            var attempts = await _attempts.GetByJobAsync(jobId, ct);
            return attempts.Select(a => new WorkerJobAttemptDto
            {
                Id = Guid.NewGuid(), // The entity uses long, but DTO expects Guid -> we generate new for API use
                JobId = a.JobId,
                StartedAt = a.StartedAt,
                FinishedAt = a.FinishedAt,
                Success = a.Ok ?? false,
                Result = a.Ok == true ? "OK" : null,
                Error = a.Error
            });
        }

        public async Task<IEnumerable<WorkerJobDeadletterDto>> GetDeadlettersAsync(CancellationToken ct)
        {
            var items = await _deadletters.GetAllAsync(ct);
            return items.Select(d => new WorkerJobDeadletterDto
            {
                Id = d.Id,
                JobId = d.JobId ?? Guid.Empty,
                JobKind = d.JobKind.ToString(),
                PayloadJson = d.Payload?.RootElement.GetRawText(),
                Error = d.LastError,
                CreatedAt = d.DeadAt
            });
        }

        public async Task<int> CleanupAsync(TimeSpan retention, CancellationToken ct)
        {
            var cutoff = DateTime.UtcNow.Subtract(retention);
            return await _jobs.DeleteExpiredAsync(cutoff, ct);
        }
    }
}
