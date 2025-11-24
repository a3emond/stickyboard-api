using StickyBoard.Core.Models;
using StickyBoard.Core.Models.BoardsAndCards;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.BoardsAndCards.Contracts;

public interface IWorkspaceRepository : IRepository<Workspace>, ISyncRepository<Workspace>
{
    Task<IEnumerable<Workspace>> GetForUserAsync(Guid userId, CancellationToken ct);

    Task<bool> IsUserMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct);

    Task<WorkspaceRole?> GetUserRoleAsync(Guid workspaceId, Guid userId, CancellationToken ct);

    Task<IEnumerable<Workspace>> SearchByNameAsync(string query, CancellationToken ct);

    Task<IEnumerable<Guid>> GetMemberIdsAsync(Guid workspaceId, CancellationToken ct);

    Task<bool> SoftDeleteCascadeAsync(Guid workspaceId, CancellationToken ct);
}