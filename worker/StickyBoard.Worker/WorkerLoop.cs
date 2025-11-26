using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyBoard.Core.Models;
using StickyBoard.Core.Models.Automation.Jobs;
using StickyBoard.Core.Services.Automation.Workers;

namespace StickyBoard.Worker;

public sealed class WorkerLoop : BackgroundService
{
    private static readonly TimeSpan JobTimeout   = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan HeartbeatGap = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkerLoop> _logger;

    public WorkerLoop(IServiceScopeFactory scopeFactory, ILogger<WorkerLoop> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var queue = scope.ServiceProvider.GetRequiredService<WorkerQueueService>();

            try
            {
                var job = await queue.DequeueAsync(stoppingToken);

                if (job is null)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                _logger.LogInformation($"Job {job.Id} started");

                using var timeoutCts =
                    CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                timeoutCts.CancelAfter(JobTimeout);

                using var heartbeat =
                    new PeriodicTimer(HeartbeatGap);

                var heartbeatTask = Task.Run(async () =>
                {
                    while (await heartbeat.WaitForNextTickAsync(timeoutCts.Token))
                    {
                        await queue.HeartbeatAsync(job.Id, timeoutCts.Token);
                        _logger.LogDebug($"Heartbeat for job {job.Id}");
                    }
                }, timeoutCts.Token);

                try
                {
                    await ProcessAsync(job, timeoutCts.Token);

                    await queue.CompleteAsync(job.Id, timeoutCts.Token);
                    _logger.LogInformation($"Job {job.Id} completed");
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    _logger.LogError($"Job {job.Id} timed out");

                    await queue.RetryAsync(
                        job.Id,
                        60 * (job.Attempts + 1),
                        "Timeout",
                        stoppingToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Job {job.Id} failed");

                    if (job.Attempts >= 5)
                        await queue.FailAsync(job.Id, ex.Message, stoppingToken);
                    else
                        await queue.RetryAsync(
                            job.Id,
                            30 * (job.Attempts + 1),
                            ex.Message,
                            stoppingToken
                        );
                }
                finally
                {
                    heartbeat.Dispose();
                    timeoutCts.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Worker loop crash");
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogInformation("Worker stopped.");
    }

    private static Task ProcessAsync(WorkerJob job, CancellationToken ct)
    {
        return job.Kind switch
        {
            WorkerJobKind.NotificationPush     => Task.CompletedTask,
            WorkerJobKind.MentionNotify        => Task.CompletedTask,
            WorkerJobKind.AssetVariant         => Task.CompletedTask,
            WorkerJobKind.InviteEmail          => Task.CompletedTask,
            WorkerJobKind.SearchIndex          => Task.CompletedTask,
            WorkerJobKind.AnalyticsAggregate   => Task.CompletedTask,
            WorkerJobKind.Cleanup              => Task.CompletedTask,
            WorkerJobKind.CdnGarbageCollect    => Task.CompletedTask,
            _ => throw new InvalidOperationException($"Unhandled job type: {job.Kind}")
        };
    }
}
