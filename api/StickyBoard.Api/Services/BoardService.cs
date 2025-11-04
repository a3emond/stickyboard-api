using System.Text.Json;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards;

namespace StickyBoard.Api.Services;

public sealed class BoardService
{
    private readonly BoardRepository _boards;
    private readonly PermissionRepository _permissions;
    private readonly TabRepository _tabs;
    private readonly SectionRepository _sections;

    public BoardService(
        BoardRepository boards,
        PermissionRepository permissions,
        TabRepository tabs,
        SectionRepository sections)
    {
        _boards = boards;
        _permissions = permissions;
        _tabs = tabs;
        _sections = sections;
    }

    // ----------------------------------------------------------------------
    // CREATE (with default Main Tab + Root Section)
    // ----------------------------------------------------------------------
    public async Task<Guid> CreateAsync(Guid ownerId, BoardCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ValidationException("Board title is required.");

        var now = DateTime.UtcNow;

        var board = new Board
        {
            OwnerId   = ownerId,
            OrgId     = dto.OrgId,
            FolderId  = dto.FolderId,
            Title     = dto.Title.Trim(),
            Visibility= dto.Visibility,
            Theme     = JsonSerializer.SerializeToDocument(dto.Theme ?? new { }),
            Meta      = JsonSerializer.SerializeToDocument(dto.Meta  ?? new { }),
            CreatedAt = now,
            UpdatedAt = now
        };

        Guid boardId = Guid.Empty;
        Guid tabId   = Guid.Empty;

        try
        {
            // 1) Create board
            boardId = await _boards.CreateAsync(board, ct);

            // 2) Create default "Board" tab
            // IMPORTANT: use the ID returned by repository,
            // do NOT assume the Id you pass will be used.
            var newTab = new Tab
            {
                // Id intentionally not set — let repo/DB generate it
                BoardId      = boardId,
                Title        = "Board",
                TabType      = TabType.board,
                Position     = 0,
                LayoutConfig = JsonDocument.Parse("{}"),
                CreatedAt    = now,
                UpdatedAt    = now
            };

            tabId = await _tabs.CreateAsync(newTab, ct);

            // 3) Create default root section bound to the actual tabId we just received
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

            return boardId;
        }
        catch
        {
            // Best-effort cleanup to avoid orphaned rows if step 2 or 3 failed
            try
            {
                if (tabId != Guid.Empty)
                {
                    // If you have a delete-by-id for tabs, call it.
                    // Otherwise, you can add one; or rely on DB cascades if sections were created.
                    await _tabs.DeleteAsync(tabId, ct);
                }
            }
            catch { /* swallow cleanup errors */ }

            try
            {
                if (boardId != Guid.Empty)
                {
                    await _boards.DeleteAsync(boardId, ct);
                }
            }
            catch { /* swallow cleanup errors */ }

            throw; // rethrow original error (will be handled by middleware)
        }
    }

    // ----------------------------------------------------------------------
    // UPDATE
    // ----------------------------------------------------------------------
    public async Task<bool> UpdateAsync(Guid actorId, Guid boardId, BoardUpdateDto dto, CancellationToken ct)
    {
        var board = await _boards.GetByIdAsync(boardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        var perm = await _permissions.GetAsync(boardId, actorId, ct);
        var canEdit = board.OwnerId == actorId || perm?.Role is BoardRole.owner or BoardRole.editor;

        if (!canEdit)
            throw new ForbiddenException("User lacks permission to edit this board.");

        if (!string.IsNullOrWhiteSpace(dto.Title))
            board.Title = dto.Title.Trim();

        if (dto.Visibility.HasValue)
            board.Visibility = dto.Visibility.Value;

        if (dto.FolderId.HasValue)
            board.FolderId = dto.FolderId;

        if (dto.Theme is not null)
            board.Theme = JsonSerializer.SerializeToDocument(dto.Theme);

        if (dto.Meta is not null)
            board.Meta = JsonSerializer.SerializeToDocument(dto.Meta);

        board.UpdatedAt = DateTime.UtcNow;
        return await _boards.UpdateAsync(board, ct);
    }

    // ----------------------------------------------------------------------
    // DELETE (DB cascade handles everything else)
    // ----------------------------------------------------------------------
    public async Task<bool> DeleteAsync(Guid actorId, Guid boardId, CancellationToken ct)
    {
        var board = await _boards.GetByIdAsync(boardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        if (board.OwnerId != actorId)
            throw new ForbiddenException("Only board owners can delete boards.");

        return await _boards.DeleteAsync(boardId, ct);
    }

    // ----------------------------------------------------------------------
    // GET / LIST
    // ----------------------------------------------------------------------
    public async Task<IEnumerable<BoardDto>> GetMineAsync(Guid ownerId, CancellationToken ct)
    {
        var boards = await _boards.GetByOwnerAsync(ownerId, ct);
        return boards.Select(Map);
    }

    public async Task<IEnumerable<BoardDto>> GetAccessibleAsync(Guid userId, CancellationToken ct)
    {
        var boards = await _boards.GetVisibleToUserAsync(userId, ct);
        return boards.Select(Map);
    }

    public async Task<IEnumerable<BoardDto>> SearchAccessibleAsync(Guid userId, string keyword, CancellationToken ct)
    {
        var boards = await _boards.SearchVisibleByTitleAsync(userId, keyword, ct);
        return boards.Select(Map);
    }

    public async Task<BoardDto?> GetAsync(Guid actorId, Guid boardId, CancellationToken ct)
    {
        var board = await _boards.GetByIdAsync(boardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        var perm = await _permissions.GetAsync(boardId, actorId, ct);
        var canView = board.OwnerId == actorId
                      || perm != null
                      || board.Visibility == BoardVisibility.public_;

        if (!canView)
            throw new ForbiddenException("You do not have access to this board.");

        return Map(board);
    }

    // ----------------------------------------------------------------------
    // PARTIAL UPDATE HELPERS
    // ----------------------------------------------------------------------
    public async Task<bool> RenameAsync(Guid actorId, Guid boardId, string title, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("Empty title.");

        var board = await _boards.GetByIdAsync(boardId, ct) ?? throw new NotFoundException("Board not found.");
        var perm = await _permissions.GetAsync(boardId, actorId, ct);

        if (board.OwnerId != actorId && perm?.Role is not BoardRole.owner and not BoardRole.editor)
            throw new ForbiddenException("Not allowed.");

        return await _boards.RenameAsync(boardId, title.Trim(), ct);
    }

    public async Task<bool> MoveToFolderAsync(Guid actorId, Guid boardId, Guid? folderId, CancellationToken ct)
    {
        var board = await _boards.GetByIdAsync(boardId, ct) ?? throw new NotFoundException("Board not found.");
        var perm = await _permissions.GetAsync(boardId, actorId, ct);

        if (board.OwnerId != actorId && perm?.Role is not BoardRole.owner and not BoardRole.editor)
            throw new ForbiddenException("Not allowed.");

        return await _boards.MoveToFolderAsync(boardId, folderId, ct);
    }

    public async Task<bool> MoveToOrgAsync(Guid actorId, Guid boardId, Guid? orgId, CancellationToken ct)
    {
        var board = await _boards.GetByIdAsync(boardId, ct) ?? throw new NotFoundException("Board not found.");
        var perm = await _permissions.GetAsync(boardId, actorId, ct);

        if (board.OwnerId != actorId && perm?.Role is not BoardRole.owner)
            throw new ForbiddenException("Only owners can move board to org.");

        return await _boards.MoveToOrgAsync(boardId, orgId, ct);
    }

    // ----------------------------------------------------------------------
    // MAP
    // ----------------------------------------------------------------------
    private static BoardDto Map(Board b) => new()
    {
        Id        = b.Id,
        Title     = b.Title,
        OwnerId   = b.OwnerId,
        OrgId     = b.OrgId,
        FolderId  = b.FolderId,
        Visibility= b.Visibility,
        Theme     = b.Theme.Deserialize<object>() ?? new { },
        Meta      = b.Meta.Deserialize<object>()  ?? new { },
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}