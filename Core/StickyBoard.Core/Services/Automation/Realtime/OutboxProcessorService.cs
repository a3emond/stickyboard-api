using StickyBoard.Core.Models;
using StickyBoard.Core.Models.Automation.RealTime;
using StickyBoard.Core.Repositories.Automation.RealTime;

namespace StickyBoard.Core.Services.Automation.Realtime;

public sealed class OutboxProcessorService
{
    private readonly ISyncCursorRepository _cursors;
    private readonly IEventOutboxRepository _outbox;
    private readonly IRealtimePublisher _publisher;

    public OutboxProcessorService(
        IEventOutboxRepository outbox,
        ISyncCursorRepository cursors,
        IRealtimePublisher publisher)
    {
        _outbox = outbox;
        _cursors = cursors;
        _publisher = publisher;
    }

    // Worker mode: process raw batch by cursor
    public async Task<long> ProcessBatchAsync(
        long fromCursor,
        int batchSize,
        CancellationToken ct)
    {
        var events = await _outbox.GetBatchAsync(fromCursor, batchSize, ct);
        if (events.Count == 0)
            return fromCursor;

        var lastCursor = fromCursor;

        foreach (var e in events)
        {
            await _publisher.PublishAsync(e, ct);
            lastCursor = e.Cursor;
        }

        // Purge processed events up to lastCursor
        await _outbox.PurgeUpToAsync(lastCursor, ct); // TODO: consider moving purge outside of this method

        return lastCursor;
    }

    // Optional: per-user / per-scope sync (used by API if needed)
    public async Task ProcessForScopeAsync(
        Guid userId,
        SyncScopeType scope,
        Guid? scopeId,
        int batchSize,
        CancellationToken ct)
    {
        var cursor = await _cursors.GetAsync(userId, scope, scopeId, ct)
                     ?? new SyncCursor
                     {
                         UserId = userId,
                         ScopeType = scope,
                         ScopeId = scopeId,
                         LastCursor = 0
                     };

        var events = await _outbox.GetBatchAsync(cursor.LastCursor, batchSize, ct);
        if (events.Count == 0) return;

        var lastCursor = cursor.LastCursor;

        foreach (var e in events)
        {
            if (scope == SyncScopeType.Workspace && e.WorkspaceId != scopeId)
                continue;

            if (scope == SyncScopeType.Board && e.BoardId != scopeId)
                continue;

            if (scope == SyncScopeType.Inbox && e.Topic != OutboxTopic.Inbox)
                continue;

            await _publisher.PublishAsync(e, ct);
            lastCursor = e.Cursor;
        }

        await _cursors.UpsertAsync(userId, scope, scopeId, lastCursor, ct);
    }
}