using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;

namespace StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

public interface IBoardMemberRepository
{
    Task<bool> AddAsync(BoardMember entity, CancellationToken ct);

    Task<bool> RemoveAsync(Guid boardId, Guid userId, CancellationToken ct);

    Task<bool> ExistsAsync(Guid boardId, Guid userId, CancellationToken ct);

    Task<bool> UpdateRoleAsync(Guid boardId, Guid userId, WorkspaceRole role, CancellationToken ct);

    Task<WorkspaceRole?> GetRoleAsync(Guid boardId, Guid userId, CancellationToken ct);

    Task<IEnumerable<BoardMember>> GetByBoardAsync(Guid boardId, CancellationToken ct);

    Task<IEnumerable<BoardMember>> GetByUserAsync(Guid userId, CancellationToken ct);
}