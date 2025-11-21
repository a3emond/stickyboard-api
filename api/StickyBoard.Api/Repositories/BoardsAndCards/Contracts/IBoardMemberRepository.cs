using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;

namespace StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

public interface IBoardMemberRepository
{
    // Overrides only (promote / demote / block)
    Task<bool> AddOrUpdateAsync(BoardMember entity, CancellationToken ct);

    // Removes override -> falls back to workspace role
    Task<bool> RemoveAsync(Guid boardId, Guid userId, CancellationToken ct);

    // Checks if override exists (NOT effective membership)
    Task<bool> HasOverrideAsync(Guid boardId, Guid userId, CancellationToken ct);

    // Effective role (board override -> workspace fallback)
    Task<WorkspaceRole?> GetEffectiveRoleAsync(Guid boardId, Guid userId, CancellationToken ct);

    // All effective members of a board (excluding Blocked)
    Task<IEnumerable<BoardMember>> GetEffectiveByBoardAsync(Guid boardId, CancellationToken ct);

    // All boards for a user (excluding Blocked)
    Task<IEnumerable<BoardMember>> GetEffectiveByUserAsync(Guid userId, CancellationToken ct);

    // Raw overrides (admin/audit/debug)
    Task<IEnumerable<BoardMember>> GetOverridesByBoardAsync(Guid boardId, CancellationToken ct);

    Task<IEnumerable<BoardMember>> GetOverridesByUserAsync(Guid userId, CancellationToken ct);
}