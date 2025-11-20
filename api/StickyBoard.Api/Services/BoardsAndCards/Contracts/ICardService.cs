using StickyBoard.Api.DTOs.BoardsAndCards;

namespace StickyBoard.Api.Services.BoardsAndCards.Contracts;

public interface ICardService
{
    Task<CardDto> CreateAsync(Guid userId, CardCreateDto dto, CancellationToken ct);
    Task<IEnumerable<CardDto>> GetByBoardAsync(Guid boardId, CancellationToken ct);
    Task<IEnumerable<CardDto>> SearchAsync(Guid boardId, string q, CancellationToken ct);
    Task UpdateAsync(Guid cardId, Guid userId, CardUpdateDto dto, CancellationToken ct);
    Task DeleteAsync(Guid cardId, CancellationToken ct);
    Task MarkAsReadAsync(Guid cardId, Guid userId, CancellationToken ct);
    Task<DateTime?> GetLastReadAsync(Guid cardId, Guid userId, CancellationToken ct);
}