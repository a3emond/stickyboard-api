using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.Services.BoardsAndCards.Contracts;

public interface IBoardService
{
    // -------------------------------------------------------
    // BOARD LIFECYCLE
    // -------------------------------------------------------

    Task<BoardDto> CreateAsync(
        Guid workspaceId,
        Guid creatorId,
        BoardCreateDto dto,
        CancellationToken ct);

    Task<IEnumerable<BoardDto>> GetForWorkspaceAsync(
        Guid workspaceId,
        CancellationToken ct);

    Task<IEnumerable<BoardDto>> GetBoardsForUserAsync(
        Guid userId,
        CancellationToken ct);

    Task RenameAsync(
        Guid boardId,
        string title,
        CancellationToken ct);

    Task DeleteAsync(
        Guid boardId,
        CancellationToken ct);

    // -------------------------------------------------------
    // MEMBERSHIP / PERMISSIONS
    // -------------------------------------------------------

    // Set board-level override (promote / demote / block)
    Task SetBoardRoleAsync(
        Guid boardId,
        Guid userId,
        WorkspaceRole role,
        CancellationToken ct);

    // Remove override (revert to workspace role)
    Task RemoveBoardOverrideAsync(
        Guid boardId,
        Guid userId,
        CancellationToken ct);

    // Get effective members of a board
    Task<IEnumerable<BoardMemberDto>> GetMembersAsync(
        Guid boardId,
        CancellationToken ct);

    // Get user's effective role on a board
    Task<WorkspaceRole?> GetUserRoleAsync(
        Guid boardId,
        Guid userId,
        CancellationToken ct);
}