using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.Services.BoardsAndCards.Contracts;

public interface IWorkspaceService
{
    Task<WorkspaceDto> CreateAsync(Guid creatorId, WorkspaceCreateDto dto, CancellationToken ct);

    Task<IEnumerable<WorkspaceDto>> GetForUserAsync(Guid userId, CancellationToken ct);

    Task RenameAsync(Guid workspaceId, string name, CancellationToken ct);

    Task AddMemberAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken ct);

    Task RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct);

    Task ChangeRoleAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken ct);

    Task<IEnumerable<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, CancellationToken ct);

    Task DeleteAsync(Guid workspaceId, CancellationToken ct);
    Task<WorkspaceRole?> GetUserRoleAsync(Guid id, Guid userId, CancellationToken ct);
}