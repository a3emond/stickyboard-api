using System.Text.Json;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;
using StickyBoard.Api.Services.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Services.BoardsAndCards;

public sealed class BoardService : IBoardService
{
    private readonly IBoardRepository _boards;
    private readonly IBoardMemberRepository _members;

    public BoardService(
        IBoardRepository boards,
        IBoardMemberRepository members)
    {
        _boards = boards;
        _members = members;
    }

    // -------------------------------------------------------
    // CREATE
    // -------------------------------------------------------
    public async Task<BoardDto> CreateAsync(
        Guid workspaceId,
        Guid creatorId,
        BoardCreateDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ValidationException("Board title is required.");

        var board = new Board
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Title = dto.Title.Trim(),
            CreatedBy = creatorId,
            Theme = dto.Theme ?? JsonDocument.Parse("{}"),
            Meta = dto.Meta ?? JsonDocument.Parse("{}")
        };

        await _boards.CreateAsync(board, ct);

        var fresh = await _boards.GetByIdAsync(board.Id, ct)
                    ?? throw new ConflictException("Board creation failed.");

        return Map(fresh);
    }

    // -------------------------------------------------------
    // GET FOR WORKSPACE
    // -------------------------------------------------------
    public async Task<IEnumerable<BoardDto>> GetForWorkspaceAsync(
        Guid workspaceId,
        CancellationToken ct)
    {
        var list = await _boards.GetForWorkspaceAsync(workspaceId, ct);
        return list.Select(Map);
    }

    // -------------------------------------------------------
    // GET FOR USER (effective access)
    // -------------------------------------------------------
    public async Task<IEnumerable<BoardDto>> GetBoardsForUserAsync(
        Guid userId,
        CancellationToken ct)
    {
        var memberships = await _members.GetEffectiveByUserAsync(userId, ct);

        var boards = new List<Board>();

        foreach (var m in memberships)
        {
            var board = await _boards.GetByIdAsync(m.BoardId, ct);
            if (board != null)
                boards.Add(board);
        }

        return boards.Select(Map);
    }

    // -------------------------------------------------------
    // RENAME
    // -------------------------------------------------------
    public async Task RenameAsync(
        Guid boardId,
        string title,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("Board title is required.");

        var board = await _boards.GetByIdAsync(boardId, ct)
                    ?? throw new NotFoundException("Board not found.");

        board.Title = title.Trim();

        var ok = await _boards.UpdateAsync(board, ct);
        if (!ok)
            throw new ConflictException("Board update conflict.");
    }

    // -------------------------------------------------------
    // DELETE
    // -------------------------------------------------------
    public async Task DeleteAsync(Guid boardId, CancellationToken ct)
    {
        var ok = await _boards.DeleteAsync(boardId, ct);
        if (!ok)
            throw new NotFoundException("Board not found or already deleted.");
    }

    // -------------------------------------------------------
    // SET ROLE / BLOCK / PROMOTE / DEMOTE
    // -------------------------------------------------------
    public async Task SetBoardRoleAsync(
        Guid boardId,
        Guid userId,
        WorkspaceRole role,
        CancellationToken ct)
    {
        if (!await _boards.ExistsAsync(boardId, ct))
            throw new NotFoundException("Board not found.");

        var member = new BoardMember
        {
            BoardId = boardId,
            UserId = userId,
            Role = role
        };

        await _members.AddOrUpdateAsync(member, ct);
    }

    // -------------------------------------------------------
    // REMOVE OVERRIDE (fallback to workspace)
    // -------------------------------------------------------
    public async Task RemoveBoardOverrideAsync(
        Guid boardId,
        Guid userId,
        CancellationToken ct)
    {
        var ok = await _members.RemoveAsync(boardId, userId, ct);

        if (!ok)
            throw new NotFoundException("Board override not found.");
    }

    // -------------------------------------------------------
    // GET EFFECTIVE MEMBERS (excluding blocked)
    // -------------------------------------------------------
    public async Task<IEnumerable<BoardMemberDto>> GetMembersAsync(
        Guid boardId,
        CancellationToken ct)
    {
        var members = await _members.GetEffectiveByBoardAsync(boardId, ct);

        return members.Select(m => new BoardMemberDto
        {
            UserId = m.UserId,
            Role = m.Role
        });
    }

    // -------------------------------------------------------
    // GET USER ROLE (effective)
    // -------------------------------------------------------
    public async Task<WorkspaceRole?> GetUserRoleAsync(
        Guid boardId,
        Guid userId,
        CancellationToken ct)
    {
        return await _members.GetEffectiveRoleAsync(boardId, userId, ct);
    }

    // -------------------------------------------------------
    // MAPPING
    // -------------------------------------------------------
    private static BoardDto Map(Board b) => new()
    {
        Id = b.Id,
        WorkspaceId = b.WorkspaceId,
        Title = b.Title,
        Theme = b.Theme,
        Meta = b.Meta,
        CreatedBy = b.CreatedBy,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}