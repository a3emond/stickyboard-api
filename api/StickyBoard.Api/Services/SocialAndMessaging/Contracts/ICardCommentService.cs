using StickyBoard.Api.DTOs.SocialAndMessaging;

namespace StickyBoard.Api.Services.SocialAndMessaging.Contracts;

public interface ICardCommentService
{
    Task<IEnumerable<CardCommentDto>> GetByCardAsync(Guid cardId, CancellationToken ct);
    Task<IEnumerable<CardCommentDto>> GetThreadAsync(Guid cardId, Guid rootCommentId, CancellationToken ct);

    Task<Guid> CreateAsync(Guid cardId, Guid userId, CardCommentCreateDto dto, CancellationToken ct);
    Task<bool> UpdateAsync(Guid commentId, Guid userId, CardCommentUpdateDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(Guid commentId, Guid userId, CancellationToken ct);
}