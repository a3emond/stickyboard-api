using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;
using StickyBoard.Api.Services.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Services.SocialAndMessaging;

public sealed class MessageService : IMessageService
{
    private readonly IMessageRepository _messages;

    public MessageService(IMessageRepository messages)
    {
        _messages = messages;
    }

    // ------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------
    public async Task<MessageDto> CreateAsync(Guid senderId, MessageCreateDto dto, CancellationToken ct)
    {
        var entity = new Message
        {
            Channel = dto.Channel,
            BoardId = dto.BoardId,
            ViewId = dto.ViewId,
            ParentId = dto.ParentId,
            SenderId = senderId,
            Content = dto.Content
        };

        var id = await _messages.CreateAsync(entity, ct);
        var created = await _messages.GetByIdAsync(id, ct);

        return Map(created!);
    }

    // ------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------
    public async Task<bool> UpdateAsync(Guid messageId, Guid userId, MessageUpdateDto dto, CancellationToken ct)
    {
        var existing = await _messages.GetByIdAsync(messageId, ct);
        if (existing is null || existing.SenderId != userId)
            return false;

        existing.Content = dto.Content;
        existing.ParentId = dto.ParentId;

        return await _messages.UpdateAsync(existing, ct);
    }

    // ------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------
    public Task<bool> DeleteAsync(Guid messageId, CancellationToken ct)
        => _messages.DeleteAsync(messageId, ct);

    // ------------------------------------------------------------
    // READ
    // ------------------------------------------------------------
    public async Task<IEnumerable<MessageDto>> GetByBoardAsync(Guid boardId, CancellationToken ct)
    {
        var list = await _messages.GetByBoardAsync(boardId, ct);
        return list.Select(Map);
    }

    public async Task<IEnumerable<MessageDto>> GetByViewAsync(Guid viewId, CancellationToken ct)
    {
        var list = await _messages.GetByViewAsync(viewId, ct);
        return list.Select(Map);
    }

    public async Task<MessageDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var m = await _messages.GetByIdAsync(id, ct);
        return m is null ? null : Map(m);
    }

    // ------------------------------------------------------------
    // Mapping
    // ------------------------------------------------------------
    private static MessageDto Map(Message m) => new()
    {
        Id = m.Id,
        Channel = m.Channel,
        BoardId = m.BoardId,
        ViewId = m.ViewId,
        SenderId = m.SenderId,
        ParentId = m.ParentId,
        Content = m.Content,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };
}
