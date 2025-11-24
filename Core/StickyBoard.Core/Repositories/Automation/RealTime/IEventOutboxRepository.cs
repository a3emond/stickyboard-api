using StickyBoard.Core.Models.Automation.RealTime;

namespace StickyBoard.Core.Repositories.Automation.RealTime;

public interface IEventOutboxRepository
{
    Task<IReadOnlyList<EventOutbox>> GetBatchAsync(long afterCursor, int limit, CancellationToken ct);

    Task PurgeUpToAsync(long cursor, CancellationToken ct);
}