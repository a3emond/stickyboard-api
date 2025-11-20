using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

public interface IBoardRepository : IRepository<Board>
{
    Task<IEnumerable<Board>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct);
}