using StickyBoard.Api.Models.Worker;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Enums;
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
        public async Task<Guid> EnqueueAsync(JobKind kind, object payload, short priority = 0, short maxAttempts = 3, CancellationToken ct = default)
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
        public async Task<IEnumerable<WorkerJob>> GetQueuedAsync(CancellationToken ct)
            => await _jobs.GetQueuedAsync(ct);

        public async Task<IEnumerable<WorkerJobAttempt>> GetAttemptsAsync(Guid jobId, CancellationToken ct)
            => await _attempts.GetByJobAsync(jobId, ct);

        public async Task<IEnumerable<WorkerJobDeadletter>> GetDeadlettersAsync(CancellationToken ct)
            => await _deadletters.GetAllAsync(ct);

        public async Task<int> CleanupAsync(TimeSpan retention, CancellationToken ct)
        {
            var cutoff = DateTime.UtcNow.Subtract(retention);
            return await _jobs.DeleteExpiredAsync(cutoff, ct);
        }
    }
}
