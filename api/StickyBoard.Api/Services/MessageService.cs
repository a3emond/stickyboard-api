using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.SocialAndMessaging;

namespace StickyBoard.Api.Services;

public sealed class MessageService
{
    private readonly MessageRepository _messages;

    public MessageService(MessageRepository messages)
    {
        _messages = messages;
    }

    public async Task<Guid> SendAsync(Guid senderId, SendMessageDto dto, CancellationToken ct)
    {
        if (dto.Body is null || dto.Body.Trim().Length == 0)
            throw new ValidationException("Message body is required.");

        var entity = new Message
        {
            SenderId = senderId,
            ReceiverId = dto.ReceiverId,
            Subject = dto.Subject,
            Body = dto.Body,
            Type = dto.Type,
            RelatedBoard = dto.RelatedBoardId,
            RelatedOrg = dto.RelatedOrgId,
            Status = MessageStatus.unread,
            CreatedAt = DateTime.UtcNow
        };

        return await _messages.CreateAsync(entity, ct);
    }

    public async Task<IEnumerable<MessageDto>> GetInboxAsync(Guid userId, CancellationToken ct)
    {
        var items = await _messages.GetInboxAsync(userId, ct);
        return items.Select(Map);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct)
        => _messages.GetUnreadCountAsync(userId, ct);

    public async Task<bool> UpdateStatusAsync(Guid userId, Guid messageId, MessageStatus status, CancellationToken ct)
    {
        var msg = await _messages.GetByIdAsync(messageId, ct)
                 ?? throw new NotFoundException("Message not found.");

        if (msg.ReceiverId != userId)
            throw new ForbiddenException("Cannot modify someone else's message.");

        msg.Status = status;
        return await _messages.UpdateAsync(msg, ct);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid messageId, CancellationToken ct)
    {
        var msg = await _messages.GetByIdAsync(messageId, ct)
                 ?? throw new NotFoundException("Message not found.");

        if (msg.ReceiverId != userId)
            throw new ForbiddenException("Cannot delete someone else's message.");

        return await _messages.DeleteAsync(messageId, ct);
    }

    private static MessageDto Map(Message m) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        ReceiverId = m.ReceiverId,
        Subject = m.Subject,
        Body = m.Body,
        Type = m.Type,
        RelatedBoardId = m.RelatedBoard,
        RelatedOrgId = m.RelatedOrg,
        Status = m.Status,
        CreatedAt = m.CreatedAt
    };
}
