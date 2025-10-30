using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyBoard.Api.Models.Worker;
using StickyBoard.Api.Repositories;
using System.Collections.Concurrent;

public sealed class WorkerLoop : BackgroundService
{
    private readonly ILogger<WorkerLoop> _log;
    private readonly IServiceProvider _root;

    private readonly ConcurrentQueue<WorkerJob> _pendingJobs = new();

    private const int PollIntervalMs = 5000;
    private const int DispatchIntervalMs = 1000;

    public WorkerLoop(ILogger<WorkerLoop> log, IServiceProvider root)
    {
        _log = log;
        _root = root;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("StickyBoard Worker started (dual-loop prototype).");

        // Kick off both loops
        var fetcher = Task.Run(() => FetchLoopAsync(stoppingToken), stoppingToken);
        var dispatcher = Task.Run(() => DispatchLoopAsync(stoppingToken), stoppingToken);

        await Task.WhenAll(fetcher, dispatcher);
    }

    // ------------------------------------------------------------
    // 1. FETCH LOOP
    // ------------------------------------------------------------
    private async Task FetchLoopAsync(CancellationToken ct)
    {
        int cycle = 0;

        while (!ct.IsCancellationRequested)
        {
            cycle++;
            _log.LogInformation("[FETCH #{Cycle}] Starting new polling cycle...", cycle);

            try
            {
                using var scope = _root.CreateScope();
                var jobsRepo = scope.ServiceProvider.GetRequiredService<WorkerJobRepository>();

                var jobs = await jobsRepo.GetQueuedAsync(ct);
                int added = 0;

                foreach (var job in jobs)
                {
                    if (!_pendingJobs.Any(j => j.Id == job.Id))
                    {
                        _pendingJobs.Enqueue(job);
                        added++;
                    }
                }

                if (added > 0)
                    _log.LogInformation("[FETCH #{Cycle}] Added {Count} new jobs (total pending: {Pending})", cycle, added, _pendingJobs.Count);
                else
                    _log.LogDebug("[FETCH #{Cycle}] No new jobs found (total pending: {Pending})", cycle, _pendingJobs.Count);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[FETCH #{Cycle}] Failed: {Error}", cycle, ex.Message);
            }

            _log.LogDebug("[FETCH #{Cycle}] Sleeping {Delay}ms before next poll...", cycle, PollIntervalMs);
            await Task.Delay(PollIntervalMs, ct);
        }
    }

    // ------------------------------------------------------------
    // 2. DISPATCH LOOP
    // ------------------------------------------------------------
    private async Task DispatchLoopAsync(CancellationToken ct)
    {
        int dispatchCount = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_pendingJobs.TryDequeue(out var job))
                {
                    dispatchCount++;
                    _log.LogInformation("[DISPATCH #{Dispatch}] Dequeued job {Id} ({Kind}). Pending left: {Pending}",
                        dispatchCount, job.Id, job.JobKind, _pendingJobs.Count);

                    _ = Task.Run(() => ExecuteJobAsync(job, dispatchCount, ct), ct);
                }
                else
                {
                    _log.LogTrace("[DISPATCH] No job to dispatch. Sleeping {Delay}ms...", DispatchIntervalMs);
                    await Task.Delay(DispatchIntervalMs, ct);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[DISPATCH] Loop crashed: {Error}", ex.Message);
                await Task.Delay(2000, ct);
            }
        }
    }

    // ------------------------------------------------------------
    // 3. JOB EXECUTION
    // ------------------------------------------------------------
    private async Task ExecuteJobAsync(WorkerJob job, int dispatchIndex, CancellationToken ct)
    {
        var jobLabel = $"{job.JobKind}:{job.Id.ToString()[..8]}";
        _log.LogInformation("[EXEC #{Idx}] Starting job {Label}", dispatchIndex, jobLabel);

        try
        {
            using var scope = _root.CreateScope();
            var jobs = scope.ServiceProvider.GetRequiredService<WorkerJobRepository>();
            var attempts = scope.ServiceProvider.GetRequiredService<WorkerJobAttemptRepository>();

            var attempt = new WorkerJobAttempt
            {
                JobId = job.Id,
                StartedAt = DateTime.UtcNow
            };

            await attempts.CreateAsync(attempt, ct);
            _log.LogDebug("[EXEC #{Idx}] Attempt recorded in DB", dispatchIndex);

            // Simulated work
            _log.LogDebug("[EXEC #{Idx}] Performing fake work...", dispatchIndex);
            await Task.Delay(1500, ct);

            job.Status = StickyBoard.Api.Models.Enums.JobStatus.succeeded;
            await jobs.UpdateAsync(job, ct);

            attempt.Ok = true;
            attempt.FinishedAt = DateTime.UtcNow;
            await attempts.UpdateAsync(attempt, ct);

            _log.LogInformation("[EXEC #{Idx}] Job {Label} succeeded", dispatchIndex, jobLabel);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[EXEC #{Idx}] Job {Label} failed: {Error}", dispatchIndex, jobLabel, ex.Message);
        }
    }
}
