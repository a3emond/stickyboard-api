using StickyBoard.Core.Models.BoardsAndCards;
using StickyBoard.Core.Repositories.Base;

namespace StickyBoard.Core.Repositories.BoardsAndCards.Contracts;

public interface ICardRepository : IRepository<Card>
{
    Task<IEnumerable<Card>> GetByBoardAsync(Guid boardId, CancellationToken ct);
    Task<IEnumerable<Card>> SearchAsync(Guid boardId, string query, CancellationToken ct);
}