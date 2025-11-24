using StickyBoard.Core.Models.BoardsAndCards;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.BoardsAndCards.Contracts;

public interface IViewRepository : IRepository<View>
{
    Task<IEnumerable<View>> GetForBoardAsync(Guid boardId, CancellationToken ct);
}