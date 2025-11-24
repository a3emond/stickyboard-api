using StickyBoard.Core.DTOs.SocialAndMessaging;

namespace StickyBoard.Core.Services.SocialAndMessaging.Contracts;

public interface IInboxMessageService
{
    Task<InboxMessageDto> SendAsync(Guid senderId, InboxMessageCreateDto dto, CancellationToken ct);

    Task<IEnumerable<InboxMessageDto>> GetForUserAsync(Guid userId, CancellationToken ct);

    Task<bool> MarkAsReadAsync(Guid messageId, CancellationToken ct);
}