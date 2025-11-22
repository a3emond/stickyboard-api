using StickyBoard.Api.Common;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;
using StickyBoard.Api.Services.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Services.SocialAndMessaging;

public sealed class CardCommentService : ICardCommentService
{
    private readonly ICardRepository _cards;
    private readonly ICardCommentRepository _comments;

    public CardCommentService(
        ICardRepository cards,
        ICardCommentRepository comments)
    {
        _cards = cards;
        _comments = comments;
    }

    // --------------------------------------
    // READ
    // --------------------------------------

    public async Task<IEnumerable<CardCommentDto>> GetByCardAsync(Guid cardId, CancellationToken ct)
    {
        if (!await _cards.ExistsAsync(cardId, ct))
            throw new NotFoundException("Card not found.");

        var list = await _comments.GetByCardIdAsync(cardId, ct);
        return list.Select(MapToDto);
    }

    public async Task<IEnumerable<CardCommentDto>> GetThreadAsync(Guid cardId, Guid rootCommentId, CancellationToken ct)
    {
        if (!await _cards.ExistsAsync(cardId, ct))
            throw new NotFoundException("Card not found.");

        var list = await _comments.GetThreadAsync(cardId, rootCommentId, ct);
        return list.Select(MapToDto);
    }

    // --------------------------------------
    // WRITE
    // --------------------------------------

    public async Task<Guid> CreateAsync(Guid cardId, Guid userId, CardCommentCreateDto dto, CancellationToken ct)
    {
        if (!await _cards.ExistsAsync(cardId, ct))
            throw new NotFoundException("Card not found.");

        var entity = new CardComment
        {
            CardId = cardId,
            UserId = userId,
            ParentId = dto.ParentId,
            Content = dto.Content
        };

        return await _comments.CreateAsync(entity, ct);
    }

    public async Task<bool> UpdateAsync(Guid commentId, Guid userId, CardCommentUpdateDto dto, CancellationToken ct)
    {
        var existing = await _comments.GetByIdAsync(commentId, ct)
            ?? throw new NotFoundException("Comment not found.");

        if (existing.UserId != userId)
            throw new ForbiddenException("Cannot edit this comment.");

        existing.Content = dto.Content;

        return await _comments.UpdateAsync(existing, ct);
    }

    public async Task<bool> DeleteAsync(Guid commentId, Guid userId, CancellationToken ct)
    {
        var existing = await _comments.GetByIdAsync(commentId, ct)
            ?? throw new NotFoundException("Comment not found.");

        if (existing.UserId != userId)
            throw new ForbiddenException("Cannot delete this comment.");

        return await _comments.DeleteAsync(commentId, ct);
    }

    private static CardCommentDto MapToDto(CardComment c) => new()
    {
        Id = c.Id,
        CardId = c.CardId,
        ParentId = c.ParentId,
        UserId = c.UserId,
        Content = c.Content,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
