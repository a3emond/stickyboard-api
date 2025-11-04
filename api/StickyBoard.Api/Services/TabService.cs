using System.Text.Json;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards;

namespace StickyBoard.Api.Services;

public sealed class TabService
{
    private readonly TabRepository _tabs;
    private readonly SectionRepository _sections;
    private readonly BoardRepository _boards;
    private readonly PermissionRepository _permissions;

    public TabService(TabRepository tabs, SectionRepository sections, BoardRepository boards, PermissionRepository permissions)
    {
        _tabs = tabs;
        _sections = sections;
        _boards = boards;
        _permissions = permissions;
    }

    private async Task EnsureEditorAsync(Guid userId, Guid boardId, CancellationToken ct)
    {
        var board = await _boards.GetByIdAsync(boardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        if (board.OwnerId == userId) return;

        var perm = await _permissions.GetAsync(boardId, userId, ct);
        if (perm?.Role is not BoardRole.owner and not BoardRole.editor)
            throw new ForbiddenException("Insufficient permission.");
    }

    // --------------------------------------------------------
    // Create Tab
    // --------------------------------------------------------
    public async Task<Guid> CreateAsync(Guid userId, TabCreateDto dto, CancellationToken ct)
    {
        await EnsureEditorAsync(userId, dto.BoardId, ct);

        var newPosition = await _tabs.GetMaxPositionAsync(dto.BoardId, ct) + 1;
        var now = DateTime.UtcNow;

        var tab = new Tab
        {
            BoardId = dto.BoardId,
            Title = dto.Title?.Trim() ?? "",
            TabType = dto.TabType,
            Position = newPosition,
            LayoutConfig = JsonSerializer.SerializeToDocument(dto.Layout ?? new { }),
            CreatedAt = now,
            UpdatedAt = now
        };

        Guid tabId = await _tabs.CreateAsync(tab, ct);
        

        // 2) Create default root section bound to the actual tabId we just received
        var rootSection = new Section
        {
            // Id intentionally not set — let repo/DB generate it
            TabId           = tabId,
            ParentSectionId = null,
            Title           = "Root",
            Position        = 0,
            LayoutMeta      = JsonDocument.Parse("{}"),
            CreatedAt       = now,
            UpdatedAt       = now
        };

        await _sections.CreateAsync(rootSection, ct);
        return tab.Id;
    }

    // --------------------------------------------------------
    // Update Tab (Title / Type / Layout)
    // --------------------------------------------------------
    public async Task<bool> UpdateAsync(Guid userId, Guid tabId, TabUpdateDto dto, CancellationToken ct)
    {
        var tab = await _tabs.GetByIdAsync(tabId, ct) ?? throw new NotFoundException("Tab not found.");

        await EnsureEditorAsync(userId, tab.BoardId, ct);

        if (!string.IsNullOrWhiteSpace(dto.Title))
            tab.Title = dto.Title.Trim();

        if (dto.TabType.HasValue)
            tab.TabType = dto.TabType.Value;

        if (dto.Layout != null)
            tab.LayoutConfig = JsonSerializer.SerializeToDocument(dto.Layout);

        tab.UpdatedAt = DateTime.UtcNow;
        return await _tabs.UpdateAsync(tab, ct);
    }

    // --------------------------------------------------------
    // Move Tab
    // --------------------------------------------------------
    public async Task<bool> MoveAsync(Guid userId, Guid tabId, int newPosition, CancellationToken ct)
    {
        var tab = await _tabs.GetByIdAsync(tabId, ct) ?? throw new NotFoundException("Tab not found.");

        await EnsureEditorAsync(userId, tab.BoardId, ct);

        return await _tabs.MoveAsync(tabId, tab.BoardId, newPosition, ct);
    }

    // --------------------------------------------------------
    // Delete Tab
    // --------------------------------------------------------
    public async Task<bool> DeleteAsync(Guid userId, Guid tabId, CancellationToken ct)
    {
        var tab = await _tabs.GetByIdAsync(tabId, ct) ?? throw new NotFoundException("Tab not found.");

        await EnsureEditorAsync(userId, tab.BoardId, ct);

        return await _tabs.DeleteAsync(tabId, ct);
    }

    // --------------------------------------------------------
    // Get Tabs for Board
    // --------------------------------------------------------
    public async Task<IEnumerable<TabDto>> GetForBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
    {
        var board = await _boards.GetByIdAsync(boardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        var perm = await _permissions.GetAsync(boardId, userId, ct);
        if (board.OwnerId != userId && perm == null && board.Visibility != BoardVisibility.public_)
            throw new ForbiddenException("You do not have access to this board.");

        var tabs = await _tabs.GetByBoardAsync(boardId, ct);
        return tabs.Select(t => new TabDto
        {
            Id = t.Id,
            BoardId = t.BoardId,
            Title = t.Title,
            TabType = t.TabType,
            Position = t.Position,
            Layout = t.LayoutConfig.Deserialize<object>() ?? new { }
        });
    }
}
