using System.Text.Json;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards;

namespace StickyBoard.Api.Services;

public sealed class CardService
{
    private readonly CardRepository _cards;
    private readonly BoardRepository _boards;
    private readonly PermissionRepository _permissions;

    public CardService(CardRepository cards, BoardRepository boards, PermissionRepository permissions)
    {
        _cards = cards;
        _boards = boards;
        _permissions = permissions;
    }

    private async Task<bool> EnsureWriteAccess(Guid boardId, Guid userId, CancellationToken ct)
    {
        var board = await _boards.GetByIdAsync(boardId, ct) 
                    ?? throw new NotFoundException("Board not found.");

        if (board.OwnerId == userId)
            return true;

        var perm = await _permissions.GetAsync(boardId, userId, ct);
        return perm?.Role is BoardRole.owner or BoardRole.editor;
    }

    private async Task<bool> EnsureReadAccess(Guid boardId, Guid userId, CancellationToken ct)
    {
        var board = await _boards.GetByIdAsync(boardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        if (board.OwnerId == userId || board.Visibility == BoardVisibility.public_)
            return true;

        var perm = await _permissions.GetAsync(boardId, userId, ct);
        return perm != null;
    }

    // -------------------------------------------------------------
    // CREATE
    // -------------------------------------------------------------
    private async Task<int> GetNextPosition(Guid tabId, Guid? sectionId, CancellationToken ct)
    {
        // Pull siblings in the same container (section if present, else tab)
        IEnumerable<Card> siblings = sectionId.HasValue
            ? await _cards.GetBySectionAsync(sectionId.Value, ct)
            : await _cards.GetByTabAsync(tabId, ct);

        // If there are no siblings, start at 0; otherwise max + 1
        // DefaultIfEmpty(-1) ensures Max() always has a value.
        return siblings
            .Select(c => c.Position)
            .DefaultIfEmpty(-1)
            .Max() + 1;
    }

    public async Task<Guid> CreateAsync(Guid userId, CardCreateDto dto, CancellationToken ct)
    {
        if (!await EnsureWriteAccess(dto.BoardId, userId, ct))
            throw new ForbiddenException("No write permission.");

        int position = dto.Position ?? await GetNextPosition(dto.TabId, dto.SectionId, ct);

        var card = new Card
        {
            Id         = Guid.NewGuid(),
            BoardId    = dto.BoardId,
            TabId      = dto.TabId,
            SectionId  = dto.SectionId,
            Type       = dto.Type,
            Title      = dto.Title?.Trim(),
            Content    = JsonSerializer.SerializeToDocument(dto.Content ?? new { }),
            InkData    = dto.InkData != null ? JsonSerializer.SerializeToDocument(dto.InkData) : null,
            DueDate    = dto.DueDate,
            Priority   = dto.Priority,
            Status     = CardStatus.open,
            Tags       = dto.Tags?.ToArray() ?? Array.Empty<string>(),
            CreatedBy  = userId,
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow,
            Position   = position
        };

        return await _cards.CreateAsync(card, ct);
    }

    // -------------------------------------------------------------
    // UPDATE
    // -------------------------------------------------------------
    public async Task<bool> UpdateAsync(Guid userId, Guid cardId, CardUpdateDto dto, CancellationToken ct)
    {
        var existing = await _cards.GetByIdAsync(cardId, ct)
                       ?? throw new NotFoundException("Card not found.");

        if (!await EnsureWriteAccess(existing.BoardId, userId, ct))
            throw new ForbiddenException("No write permission.");

        existing.Title       = dto.Title?.Trim() ?? existing.Title;
        existing.Content     = dto.Content != null 
            ? JsonSerializer.SerializeToDocument(dto.Content) 
            : existing.Content;
        existing.InkData     = dto.InkData != null
            ? JsonSerializer.SerializeToDocument(dto.InkData)
            : existing.InkData;
        existing.Tags        = dto.Tags?.ToArray() ?? existing.Tags;
        existing.Status      = dto.Status ?? existing.Status;
        existing.Priority    = dto.Priority != default ? dto.Priority : existing.Priority;
        existing.AssigneeId  = dto.AssigneeId ?? existing.AssigneeId;
        existing.DueDate     = dto.DueDate ?? existing.DueDate;
        existing.StartTime   = dto.StartTime ?? existing.StartTime;
        existing.EndTime     = dto.EndTime ?? existing.EndTime;
        existing.SectionId   = dto.SectionId ?? existing.SectionId;
        existing.TabId       = dto.TabId ?? existing.TabId;

        if (dto.Position.HasValue)
            existing.Position = dto.Position.Value;

        existing.UpdatedAt   = DateTime.UtcNow;

        return await _cards.UpdateAsync(existing, ct);
    }

    // -------------------------------------------------------------
    // DELETE
    // -------------------------------------------------------------
    public async Task<bool> DeleteAsync(Guid userId, Guid cardId, CancellationToken ct)
    {
        var card = await _cards.GetByIdAsync(cardId, ct)
                    ?? throw new NotFoundException("Card not found.");

        if (!await EnsureWriteAccess(card.BoardId, userId, ct))
            throw new ForbiddenException("No write permission.");

        return await _cards.DeleteAsync(cardId, ct);
    }

    // -------------------------------------------------------------
    // GET
    // -------------------------------------------------------------
    public async Task<CardDto> GetAsync(Guid userId, Guid cardId, CancellationToken ct)
    {
        var card = await _cards.GetByIdAsync(cardId, ct)
                    ?? throw new NotFoundException("Card not found.");

        if (!await EnsureReadAccess(card.BoardId, userId, ct))
            throw new ForbiddenException("No access.");

        return Map(card);
    }

    // -------------------------------------------------------------
    // LIST BY TAB / SECTION
    // -------------------------------------------------------------
    public async Task<IEnumerable<CardDto>> GetByTabAsync(Guid userId, Guid tabId, CancellationToken ct)
    {
        var cards = await _cards.GetByTabAsync(tabId, ct);
        if (!cards.Any()) return Enumerable.Empty<CardDto>();

        var boardId = cards.First().BoardId;
        if (!await EnsureReadAccess(boardId, userId, ct))
            throw new ForbiddenException("No access.");

        return cards.Select(Map);
    }

    public async Task<IEnumerable<CardDto>> GetBySectionAsync(Guid userId, Guid sectionId, CancellationToken ct)
    {
        var cards = await _cards.GetBySectionAsync(sectionId, ct);
        if (!cards.Any()) return Enumerable.Empty<CardDto>();

        var boardId = cards.First().BoardId;
        if (!await EnsureReadAccess(boardId, userId, ct))
            throw new ForbiddenException("No access.");

        return cards.Select(Map);
    }

    // -------------------------------------------------------------
    // REORDER
    // -------------------------------------------------------------
    public async Task ReorderAsync(Guid userId, IEnumerable<(Guid cardId, int position)> requests, CancellationToken ct)
    {
        // Validate all permissions before writing
        foreach (var (cardId, _) in requests)
        {
            var card = await _cards.GetByIdAsync(cardId, ct)
                       ?? throw new NotFoundException("Card not found.");

            if (!await EnsureWriteAccess(card.BoardId, userId, ct))
                throw new ForbiddenException("No write permission.");
        }

        await _cards.BulkUpdatePositionsAsync(requests, ct);
    }

    // -------------------------------------------------------------
    // MAP
    // -------------------------------------------------------------
    private static CardDto Map(Card e) => new()
    {
        Id         = e.Id,
        BoardId    = e.BoardId,
        TabId      = e.TabId ?? Guid.Empty,
        SectionId  = e.SectionId,
        Type       = e.Type,
        Title      = e.Title,
        Content    = e.Content.Deserialize<object>() ?? new { },
        InkData    = e.InkData?.Deserialize<object>(),
        Tags       = e.Tags.ToList(),
        Status     = e.Status,
        Priority   = e.Priority ?? 0,
        AssigneeId = e.AssigneeId,
        DueDate    = e.DueDate,
        StartTime  = e.StartTime,
        EndTime    = e.EndTime,
        UpdatedAt  = e.UpdatedAt
    };
}