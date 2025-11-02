using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Repositories.BoardsAndCards;

namespace StickyBoard.Api.Services;

public sealed class CardCommentService
{
    private readonly CardCommentRepository _repo;
    private readonly PermissionRepository _perm;
    private readonly CardRepository _cards;

    public CardCommentService(CardCommentRepository repo, PermissionRepository perm, CardRepository cards)
    {
        _repo = repo;
        _perm = perm;
        _cards = cards;
    }

    public async Task<IEnumerable<CardCommentDto>> GetForCardAsync(Guid actorId, Guid cardId, CancellationToken ct)
    {
        var card = await _cards.GetByIdAsync(cardId, ct)
                   ?? throw new NotFoundException("Card not found.");

        var perm = await _perm.GetAsync(card.BoardId, actorId, ct);
        if (perm is null)
            throw new ForbiddenException("Not allowed.");

        var list = await _repo.GetByCardAsync(cardId, ct);

        return list.Select(c => new CardCommentDto
        {
            Id = c.Id,
            CardId = c.CardId,
            User = new UserDto { Id = c.UserId },
            Content = c.Content,
            CreatedAt = c.CreatedAt
        });
    }

    public async Task<Guid> CreateAsync(Guid actorId, Guid cardId, CardCommentCreateDto dto, CancellationToken ct)
    {
        var card = await _cards.GetByIdAsync(cardId, ct)
                   ?? throw new NotFoundException("Card not found.");

        var perm = await _perm.GetAsync(card.BoardId, actorId, ct);
        if (perm is null)
            throw new ForbiddenException("Not allowed.");

        var comment = new CardComment
        {
            CardId = cardId,
            UserId = actorId,
            Content = dto.Content
        };

        return await _repo.CreateAsync(comment, ct);
    }
}