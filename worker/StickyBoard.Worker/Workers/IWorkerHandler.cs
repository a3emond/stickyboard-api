using StickyBoard.Core.Models;

namespace StickyBoard.Worker.Workers;

using Core.Models.Automation.Jobs;

public interface IWorkerHandler
{
    WorkerJobKind Kind { get; }
    Task HandleAsync(WorkerJob job, CancellationToken ct);
}
