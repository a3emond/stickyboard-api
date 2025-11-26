using StickyBoard.Core.Models.Automation.Jobs;

namespace StickyBoard.Core.Repositories.Automation.Jobs;

public interface IWorkerJobRepository
{
    Task<long> InsertAsync(WorkerJob job, CancellationToken ct);

    Task<WorkerJob?> LockNextAvailableAsync(CancellationToken ct);

    Task MarkRunningAsync(long jobId, CancellationToken ct);

    Task MarkDoneAsync(long jobId, CancellationToken ct);

    Task MarkFailedAsync(long jobId, string error, CancellationToken ct);

    Task IncrementAttemptsAsync(long jobId, string? error, CancellationToken ct);

    Task RescheduleAsync(long jobId, DateTime nextAttemptAt, CancellationToken ct);
}