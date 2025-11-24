using StickyBoard.Core.Models.BoardsAndCards;

namespace StickyBoard.Core.Repositories.BoardsAndCards.Contracts;

public interface ICardReadRepository
{
    Task UpsertAsync(Guid cardId, Guid userId, CancellationToken ct);
    Task<CardRead?> GetAsync(Guid cardId, Guid userId, CancellationToken ct);
}