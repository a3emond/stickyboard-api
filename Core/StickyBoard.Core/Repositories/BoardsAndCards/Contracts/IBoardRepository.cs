using StickyBoard.Core.Models.BoardsAndCards;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.BoardsAndCards.Contracts;

public interface IBoardRepository : IRepository<Board>
{
    Task<IEnumerable<Board>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct);
}