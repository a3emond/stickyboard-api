using System.Text.Json;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;
using StickyBoard.Api.Services.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Services;

public sealed class ViewService : IViewService
{
    private readonly IViewRepository _views;
    private readonly IBoardRepository _boards;

    public ViewService(
        IViewRepository views,
        IBoardRepository boards)
    {
        _views = views;
        _boards = boards;
    }

    // ---------------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------------
    public async Task<ViewDto> CreateAsync(Guid boardId, ViewCreateDto dto, CancellationToken ct)
    {
        if (!await _boards.ExistsAsync(boardId, ct))
            throw new NotFoundException("Board not found.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ValidationException("View title is required.");

        var view = new View
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Title = dto.Title.Trim(),
            Type = dto.Type,
            Layout = dto.Layout ?? JsonDocument.Parse("{}"),
            Position = dto.Position,
            Version = 1
        };

        await _views.CreateAsync(view, ct);

        var fresh = await _views.GetByIdAsync(view.Id, ct)
                    ?? throw new ConflictException("View creation failed.");

        return Map(fresh);
    }

    // ---------------------------------------------------------------------
    // GET FOR BOARD
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<ViewDto>> GetForBoardAsync(Guid boardId, CancellationToken ct)
    {
        var list = await _views.GetForBoardAsync(boardId, ct);

        return list.Select(Map);
    }

    // ---------------------------------------------------------------------
    // UPDATE (VERSION-SAFE)
    // ---------------------------------------------------------------------
    public async Task UpdateAsync(Guid viewId, ViewUpdateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ValidationException("View title is required.");

        var view = await _views.GetByIdAsync(viewId, ct)
                   ?? throw new NotFoundException("View not found.");

        view.Title = dto.Title.Trim();
        view.Type = dto.Type;
        view.Layout = dto.Layout ?? JsonDocument.Parse("{}");
        view.Position = dto.Position;

        view.Version = dto.Version; // sent from client

        var ok = await _views.UpdateAsync(view, ct);

        if (!ok)
            throw new ConflictException("View update conflict. Version mismatch.");
    }

    // ---------------------------------------------------------------------
    // DELETE
    // ---------------------------------------------------------------------
    public async Task DeleteAsync(Guid viewId, CancellationToken ct)
    {
        var ok = await _views.DeleteAsync(viewId, ct);

        if (!ok)
            throw new NotFoundException("View not found or already deleted.");
    }

    // ---------------------------------------------------------------------
    // MAPPING
    // ---------------------------------------------------------------------
    private static ViewDto Map(View v) => new()
    {
        Id = v.Id,
        BoardId = v.BoardId,
        Title = v.Title,
        Type = v.Type,
        Layout = v.Layout,
        Position = v.Position,
        Version = v.Version,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt
    };
}