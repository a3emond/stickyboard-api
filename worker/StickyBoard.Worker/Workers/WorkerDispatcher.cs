using StickyBoard.Core.Models.Automation.Jobs;

namespace StickyBoard.Worker.Workers;

public sealed class WorkerDispatcher
{
    private readonly IEnumerable<IWorkerHandler> _handlers;

    public WorkerDispatcher(IEnumerable<IWorkerHandler> handlers)
    {
        _handlers = handlers;
    }

    public async Task DispatchAsync(WorkerJob job, CancellationToken ct)
    {
        var handler = _handlers.FirstOrDefault(h => h.Kind == job.Kind);

        if (handler is null)
            throw new InvalidOperationException($"No handler registered for {job.Kind}");

        await handler.HandleAsync(job, ct);
    }
}