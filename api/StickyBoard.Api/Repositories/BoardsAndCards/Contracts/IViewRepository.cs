using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

public interface IViewRepository : IRepository<View>
{
    Task<IEnumerable<View>> GetForBoardAsync(Guid boardId, CancellationToken ct);
}