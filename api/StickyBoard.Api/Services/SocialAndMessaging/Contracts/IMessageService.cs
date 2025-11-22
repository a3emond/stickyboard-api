using StickyBoard.Api.DTOs.SocialAndMessaging;

namespace StickyBoard.Api.Services.SocialAndMessaging.Contracts;

public interface IMessageService
{
    Task<MessageDto> CreateAsync(Guid senderId, MessageCreateDto dto, CancellationToken ct);

    Task<bool> UpdateAsync(Guid messageId, Guid userId, MessageUpdateDto dto, CancellationToken ct);

    Task<bool> DeleteAsync(Guid messageId, CancellationToken ct);

    Task<IEnumerable<MessageDto>> GetByBoardAsync(Guid boardId, CancellationToken ct);

    Task<IEnumerable<MessageDto>> GetByViewAsync(Guid viewId, CancellationToken ct);

    Task<MessageDto?> GetAsync(Guid id, CancellationToken ct);
}