using System.Text.Json;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards;

namespace StickyBoard.Api.Services;

public sealed class SectionService
{
    private readonly SectionRepository _sections;
    private readonly TabRepository _tabs;
    private readonly BoardRepository _boards;
    private readonly PermissionRepository _permissions;

    public SectionService(SectionRepository sections, TabRepository tabs, BoardRepository boards, PermissionRepository permissions)
    {
        _sections = sections;
        _tabs = tabs;
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
    // Create Section
    // --------------------------------------------------------
    public async Task<Guid> CreateAsync(Guid userId, SectionCreateDto dto, CancellationToken ct)
    {
        var tab = await _tabs.GetByIdAsync(dto.TabId, ct) ?? throw new NotFoundException("Tab not found.");

        await EnsureEditorAsync(userId, tab.BoardId, ct);

        var newPos = await _sections.GetMaxPositionAsync(dto.TabId, dto.ParentSectionId, ct) + 1;

        var section = new Section
        {
            TabId = dto.TabId,
            ParentSectionId = dto.ParentSectionId,
            Title = dto.Title?.Trim() ?? "",
            Position = newPos,
            LayoutMeta = JsonSerializer.SerializeToDocument(dto.Layout ?? new { }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _sections.CreateAsync(section, ct);
    }

    // --------------------------------------------------------
    // Update Section
    // --------------------------------------------------------
    public async Task<bool> UpdateAsync(Guid userId, Guid sectionId, SectionUpdateDto dto, CancellationToken ct)
    {
        var section = await _sections.GetByIdAsync(sectionId, ct)
                      ?? throw new NotFoundException("Section not found.");

        var tab = await _tabs.GetByIdAsync(section.TabId, ct) ?? throw new NotFoundException("Tab not found.");

        await EnsureEditorAsync(userId, tab.BoardId, ct);

        if (!string.IsNullOrWhiteSpace(dto.Title))
            section.Title = dto.Title.Trim();

        if (dto.Layout != null)
            section.LayoutMeta = JsonSerializer.SerializeToDocument(dto.Layout);

        section.UpdatedAt = DateTime.UtcNow;

        return await _sections.UpdateAsync(section, ct);
    }

    // --------------------------------------------------------
    // Move Section
    // --------------------------------------------------------
    public async Task<bool> MoveAsync(Guid userId, Guid sectionId, SectionMoveDto dto, CancellationToken ct)
    {
        var section = await _sections.GetByIdAsync(sectionId, ct)
                      ?? throw new NotFoundException("Section not found.");

        var tab = await _tabs.GetByIdAsync(section.TabId, ct)
                 ?? throw new NotFoundException("Tab not found.");

        await EnsureEditorAsync(userId, tab.BoardId, ct);

        return await _sections.MoveAsync(sectionId, section.TabId, dto.ParentSectionId, dto.NewPosition, ct);
    }

    // --------------------------------------------------------
    // Delete Section
    // --------------------------------------------------------
    public async Task<bool> DeleteAsync(Guid userId, Guid sectionId, CancellationToken ct)
    {
        var section = await _sections.GetByIdAsync(sectionId, ct)
                      ?? throw new NotFoundException("Section not found.");

        var tab = await _tabs.GetByIdAsync(section.TabId, ct)
                 ?? throw new NotFoundException("Tab not found.");

        await EnsureEditorAsync(userId, tab.BoardId, ct);

        return await _sections.DeleteAsync(sectionId, ct);
    }

    // --------------------------------------------------------
    // Get Sections by Tab
    // --------------------------------------------------------
    public async Task<IEnumerable<SectionDto>> GetForTabAsync(Guid userId, Guid tabId, CancellationToken ct)
    {
        var tab = await _tabs.GetByIdAsync(tabId, ct) ?? throw new NotFoundException("Tab not found.");

        var board = await _boards.GetByIdAsync(tab.BoardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        var perm = await _permissions.GetAsync(tab.BoardId, userId, ct);
        if (board.OwnerId != userId && perm == null && board.Visibility != BoardVisibility.public_)
            throw new ForbiddenException("Access denied.");

        var sections = await _sections.GetByTabAsync(tabId, ct);

        return sections.Select(s => new SectionDto
        {
            Id = s.Id,
            TabId = s.TabId,
            ParentSectionId = s.ParentSectionId,
            Title = s.Title,
            Position = s.Position,
            Layout = s.LayoutMeta.Deserialize<object>() ?? new { }
        });
    }
}
