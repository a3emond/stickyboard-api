using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;

namespace StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

public interface IWorkspaceMemberRepository
{
    Task<bool> AddAsync(WorkspaceMember entity, CancellationToken ct);

    Task<bool> UpdateRoleAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken ct);

    Task<bool> RemoveAsync(Guid workspaceId, Guid userId, CancellationToken ct);

    Task<bool> ExistsAsync(Guid workspaceId, Guid userId, CancellationToken ct);

    Task<WorkspaceRole?> GetRoleAsync(Guid workspaceId, Guid userId, CancellationToken ct);

    Task<IEnumerable<WorkspaceMember>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct);

    Task<IEnumerable<WorkspaceMember>> GetByUserAsync(Guid userId, CancellationToken ct);
}