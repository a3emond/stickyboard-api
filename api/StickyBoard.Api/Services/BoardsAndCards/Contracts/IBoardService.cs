using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.Services.BoardsAndCards.Contracts;

public interface IBoardService
{
    Task<BoardDto> CreateAsync(
        Guid workspaceId,
        Guid creatorId,
        BoardCreateDto dto,
        CancellationToken ct);

    Task<IEnumerable<BoardDto>> GetForWorkspaceAsync(
        Guid workspaceId,
        CancellationToken ct);

    Task RenameAsync(
        Guid boardId,
        string title,
        CancellationToken ct);

    Task DeleteAsync(
        Guid boardId,
        CancellationToken ct);

    Task AddMemberAsync(
        Guid boardId,
        Guid userId,
        WorkspaceRole role,
        CancellationToken ct);

    Task RemoveMemberAsync(
        Guid boardId,
        Guid userId,
        CancellationToken ct);

    Task ChangeRoleAsync(
        Guid boardId,
        Guid userId,
        WorkspaceRole role,
        CancellationToken ct);

    Task<IEnumerable<BoardMemberDto>> GetMembersAsync(
        Guid boardId,
        CancellationToken ct);

    Task<WorkspaceRole?> GetUserRoleAsync(
        Guid boardId,
        Guid userId,
        CancellationToken ct);
}