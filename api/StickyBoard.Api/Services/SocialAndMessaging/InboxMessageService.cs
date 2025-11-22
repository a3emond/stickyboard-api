using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;
using StickyBoard.Api.Services.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Services.SocialAndMessaging;

public sealed class InboxMessageService : IInboxMessageService
{
    private readonly IInboxMessageRepository _inbox;

    public InboxMessageService(IInboxMessageRepository inbox)
    {
        _inbox = inbox;
    }

    // ------------------------------------------------------------
    // SEND
    // ------------------------------------------------------------
    public async Task<InboxMessageDto> SendAsync(Guid senderId, InboxMessageCreateDto dto, CancellationToken ct)
    {
        var entity = new InboxMessage
        {
            SenderId = senderId,
            ReceiverId = dto.ReceiverId,
            Content = dto.Content
        };

        var id = await _inbox.CreateAsync(entity, ct);
        var created = await _inbox.GetByIdAsync(id, ct);

        return Map(created!);
    }

    // ------------------------------------------------------------
    // READ (for receiver)
    // ------------------------------------------------------------
    public async Task<IEnumerable<InboxMessageDto>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var list = await _inbox.GetForUserAsync(userId, ct);
        return list.Select(Map);
    }

    // ------------------------------------------------------------
    // MARK AS READ
    // ------------------------------------------------------------
    public Task<bool> MarkAsReadAsync(Guid messageId, CancellationToken ct)
        => _inbox.MarkAsReadAsync(messageId, ct);

    // ------------------------------------------------------------
    // Mapping
    // ------------------------------------------------------------
    private static InboxMessageDto Map(InboxMessage m) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        ReceiverId = m.ReceiverId,
        Content = m.Content,
        CreatedAt = m.CreatedAt,
        ReadAt = m.ReadAt
    };
}
