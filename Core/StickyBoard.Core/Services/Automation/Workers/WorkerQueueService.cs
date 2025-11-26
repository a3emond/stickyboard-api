using System.Text.Json;
using StickyBoard.Core.Models;
using StickyBoard.Core.Models.Automation.Jobs;
using StickyBoard.Core.Repositories.Automation.Jobs;

namespace StickyBoard.Core.Services.Automation.Workers;

public sealed class WorkerQueueService
{
    private static readonly Dictionary<WorkerJobKind, int> PriorityMap = new()
    {
        [WorkerJobKind.NotificationPush] = 1,
        [WorkerJobKind.MentionNotify] = 1,

        [WorkerJobKind.AssetVariant] = 3,

        [WorkerJobKind.InviteEmail] = 5,
        [WorkerJobKind.SearchIndex] = 5,

        [WorkerJobKind.AnalyticsAggregate] = 8,
        [WorkerJobKind.Cleanup] = 8,

        [WorkerJobKind.CdnGarbageCollect] = 10
    };

    private readonly IWorkerJobAttemptRepository _attempts;
    private readonly IWorkerJobRepository _jobs;

    public WorkerQueueService(
        IWorkerJobRepository jobs,
        IWorkerJobAttemptRepository attempts)
    {
        _jobs = jobs;
        _attempts = attempts;
    }

    private static int ResolvePriority(WorkerJobKind kind)
    {
        return PriorityMap.TryGetValue(kind, out var p) ? p : 5;
    }

    // ------------------------------------------------------------
    // ENQUEUE
    // ------------------------------------------------------------
    public async Task<long> EnqueueAsync(
        WorkerJobKind kind,
        JsonDocument payload,
        DateTime? availableAt,
        CancellationToken ct)
    {
        var job = new WorkerJob
        {
            Kind = kind,
            Payload = payload,
            Status = WorkerJobStatus.Queued,
            Priority = ResolvePriority(kind),
            AvailableAt = availableAt ?? DateTime.UtcNow,
            Attempts = 0
        };

        return await _jobs.InsertAsync(job, ct);
    }

    // ------------------------------------------------------------
    // DEQUEUE (smart pull)
    // ------------------------------------------------------------
    public async Task<WorkerJob?> DequeueAsync(CancellationToken ct)
    {
        var job = await _jobs.LockNextAvailableAsync(ct);

        if (job is null)
            return null;

        await _jobs.MarkRunningAsync(job.Id, ct);

        await _attempts.StartAsync(job.Id, ct);

        return job;
    }

    // ------------------------------------------------------------
    // COMPLETE SUCCESS
    // ------------------------------------------------------------
    public async Task CompleteAsync(long jobId, CancellationToken ct)
    {
        await _jobs.MarkDoneAsync(jobId, ct);
    }

    // ------------------------------------------------------------
    // COMPLETE FAILURE
    // ------------------------------------------------------------
    public async Task FailAsync(long jobId, string error, CancellationToken ct)
    {
        await _jobs.MarkFailedAsync(jobId, error, ct);
    }

    // ------------------------------------------------------------
    // REQUEUE (backoff)
    // ------------------------------------------------------------
    public async Task RetryAsync(long jobId, int delaySeconds, string? error, CancellationToken ct)
    {
        await _jobs.IncrementAttemptsAsync(jobId, error, ct);
        await _jobs.RescheduleAsync(jobId, DateTime.UtcNow.AddSeconds(delaySeconds), ct);
    }

    public async Task HeartbeatAsync(long jobId, CancellationToken ct)
    {
        await _jobs.TouchAsync(jobId, ct);
    }

}