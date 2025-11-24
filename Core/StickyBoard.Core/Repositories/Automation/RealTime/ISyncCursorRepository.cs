using StickyBoard.Core.Models;
using StickyBoard.Core.Models.Automation.RealTime;

namespace StickyBoard.Core.Repositories.Automation.RealTime;

public interface ISyncCursorRepository
{
    Task<SyncCursor?> GetAsync(
        Guid userId,
        SyncScopeType scopeType,
        Guid? scopeId,
        CancellationToken ct);

    Task UpsertAsync(
        Guid userId,
        SyncScopeType scopeType,
        Guid? scopeId,
        long lastCursor,
        CancellationToken ct);

    Task<bool> DeleteAsync(
        Guid userId,
        SyncScopeType scopeType,
        Guid? scopeId,
        CancellationToken ct);
}