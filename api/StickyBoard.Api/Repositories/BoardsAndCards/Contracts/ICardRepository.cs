using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

public interface ICardRepository : IRepository<Card>
{
    Task<IEnumerable<Card>> GetByBoardAsync(Guid boardId, CancellationToken ct);
    Task<IEnumerable<Card>> SearchAsync(Guid boardId, string query, CancellationToken ct);
}