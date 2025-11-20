using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;
using StickyBoard.Api.Services.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Services;

public sealed class CardService : ICardService
{
    private readonly ICardRepository _cards;
    private readonly IBoardRepository _boards;
    private readonly ICardReadRepository _reads;

    public CardService(ICardRepository cards, IBoardRepository boards, ICardReadRepository reads)
    {
        _cards = cards;
        _boards = boards;
        _reads = reads;
    }

    public async Task<CardDto> CreateAsync(Guid userId, CardCreateDto dto, CancellationToken ct)
    {
        if (!await _boards.ExistsAsync(dto.BoardId, ct))
            throw new NotFoundException("Board not found.");

        var card = new Card
        {
            Id = Guid.NewGuid(),
            BoardId = dto.BoardId,
            Title = dto.Title,
            Markdown = dto.Markdown?.Trim() ?? string.Empty,
            InkData = dto.InkData,
            Checklist = dto.Checklist,
            DueDate = dto.DueDate,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Priority = dto.Priority,
            Status = dto.Status,
            Tags = dto.Tags,
            Assignee = dto.Assignee,
            CreatedBy = userId,
            LastEditedBy = userId
        };

        await _cards.CreateAsync(card, ct);

        var fresh = await _cards.GetByIdAsync(card.Id, ct)
            ?? throw new ConflictException("Card creation failed.");

        return Map(fresh);
    }

    public async Task<IEnumerable<CardDto>> GetByBoardAsync(Guid boardId, CancellationToken ct)
    {
        var list = await _cards.GetByBoardAsync(boardId, ct);
        return list.Select(Map);
    }

    public async Task<IEnumerable<CardDto>> SearchAsync(Guid boardId, string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            throw new ValidationException("Search query is required.");

        var list = await _cards.SearchAsync(boardId, q, ct);
        return list.Select(Map);
    }

    public async Task UpdateAsync(Guid cardId, Guid userId, CardUpdateDto dto, CancellationToken ct)
    {
        var card = await _cards.GetByIdAsync(cardId, ct)
            ?? throw new NotFoundException("Card not found.");

        card.Title = dto.Title;
        card.Markdown = dto.Markdown?.Trim() ?? string.Empty;
        card.InkData = dto.InkData;
        card.Checklist = dto.Checklist;
        card.DueDate = dto.DueDate;
        card.StartDate = dto.StartDate;
        card.EndDate = dto.EndDate;
        card.Priority = dto.Priority;
        card.Status = dto.Status;
        card.Tags = dto.Tags;
        card.Assignee = dto.Assignee;
        card.LastEditedBy = userId;
        card.Version = dto.Version;

        var ok = await _cards.UpdateAsync(card, ct);

        if (!ok)
            throw new ConflictException("Card update conflict.");
    }

    public async Task DeleteAsync(Guid cardId, CancellationToken ct)
    {
        var ok = await _cards.DeleteAsync(cardId, ct);

        if (!ok)
            throw new NotFoundException("Card not found or already deleted.");
    }
    
    public async Task MarkAsReadAsync(Guid cardId, Guid userId, CancellationToken ct)
    {
        if (!await _cards.ExistsAsync(cardId, ct))
            throw new NotFoundException("Card not found.");

        await _reads.UpsertAsync(cardId, userId, ct);
    }

    public async Task<DateTime?> GetLastReadAsync(Guid cardId, Guid userId, CancellationToken ct)
    {
        var read = await _reads.GetAsync(cardId, userId, ct);
        return read?.LastSeenAt;
    }

    private static CardDto Map(Card c) => new()
    {
        Id = c.Id,
        BoardId = c.BoardId,
        Title = c.Title,
        Markdown = c.Markdown,
        InkData = c.InkData,
        Checklist = c.Checklist,
        DueDate = c.DueDate,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        Priority = c.Priority,
        Status = c.Status,
        Tags = c.Tags,
        Assignee = c.Assignee,
        CreatedBy = c.CreatedBy,
        LastEditedBy = c.LastEditedBy,
        Version = c.Version,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}