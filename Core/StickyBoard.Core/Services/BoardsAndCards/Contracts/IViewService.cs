using StickyBoard.Core.DTOs.BoardsAndCards;

namespace StickyBoard.Core.Services.BoardsAndCards.Contracts;

public interface IViewService
{
    Task<ViewDto> CreateAsync(Guid boardId, ViewCreateDto dto, CancellationToken ct);

    Task<IEnumerable<ViewDto>> GetForBoardAsync(Guid boardId, CancellationToken ct);

    Task UpdateAsync(Guid viewId, ViewUpdateDto dto, CancellationToken ct);

    Task DeleteAsync(Guid viewId, CancellationToken ct);
}