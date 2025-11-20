using StickyBoard.Api.Models.BoardsAndCards;

namespace StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

public interface ICardReadRepository
{
    Task UpsertAsync(Guid cardId, Guid userId, CancellationToken ct);
    Task<CardRead?> GetAsync(Guid cardId, Guid userId, CancellationToken ct);
}