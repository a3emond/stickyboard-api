using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyBoard.Core.Models.Automation.Jobs;
using StickyBoard.Core.Services.Automation.Workers;
using StickyBoard.Worker.Workers;

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
            var dispatcher = scope.ServiceProvider.GetRequiredService<WorkerDispatcher>();

            try
            {
                var job = await queue.DequeueAsync(stoppingToken);

                if (job is null)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                _logger.LogInformation("Job {JobId} started", job.Id);

                using var timeoutCts =
                    CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                timeoutCts.CancelAfter(JobTimeout);

                using var heartbeat =
                    new PeriodicTimer(HeartbeatGap);

                _ = Task.Run(async () =>
                {
                    while (await heartbeat.WaitForNextTickAsync(timeoutCts.Token))
                    {
                        await queue.HeartbeatAsync(job.Id, timeoutCts.Token);
                        _logger.LogDebug("Heartbeat for job {JobId}", job.Id);
                    }
                }, timeoutCts.Token);

                try
                {
                    await dispatcher.DispatchAsync(job, timeoutCts.Token);

                    await queue.CompleteAsync(job.Id, timeoutCts.Token);
                    _logger.LogInformation("Job {JobId} completed", job.Id);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    _logger.LogError("Job {JobId} timed out", job.Id);

                    await queue.RetryAsync(
                        job.Id,
                        60 * (job.Attempts + 1),
                        "Timeout",
                        stoppingToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job {JobId} failed", job.Id);

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
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Worker loop crash");
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogInformation("Worker stopped.");
    }
}
