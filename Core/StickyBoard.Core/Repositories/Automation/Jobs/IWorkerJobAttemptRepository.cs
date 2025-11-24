namespace StickyBoard.Core.Repositories.Automation.Jobs;

public interface IWorkerJobAttemptRepository
{
    Task<long> StartAsync(long jobId, CancellationToken ct);

    Task<bool> FinishAsync(long attemptId, string? error, CancellationToken ct);
}