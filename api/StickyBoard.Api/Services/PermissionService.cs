using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards;

namespace StickyBoard.Api.Services;

public sealed class PermissionService
{
    private readonly PermissionRepository _permissions;
    private readonly BoardRepository _boards;

    public PermissionService(PermissionRepository permissions, BoardRepository boards)
    {
        _permissions = permissions;
        _boards = boards;
    }

    private async Task<Board> GetBoardOrThrow(Guid boardId, CancellationToken ct)
    {
        var b = await _boards.GetByIdAsync(boardId, ct);
        if (b is null) throw new NotFoundException("Board not found.");
        return b;
    }

    private static void EnsureOwner(Guid actorId, Guid ownerId)
    {
        if (actorId != ownerId)
            throw new ForbiddenException("Only the board owner may manage permissions.");
    }

    private static void ValidateAssignable(BoardRole role)
    {
        if (role == BoardRole.owner)
            throw new ValidationException("Owner role cannot be assigned.");
    }

    public async Task<IEnumerable<PermissionDto>> GetByBoardAsync(Guid actorId, Guid boardId, CancellationToken ct)
    {
        var board = await GetBoardOrThrow(boardId, ct);
        EnsureOwner(actorId, board.OwnerId);

        var list = await _permissions.GetByBoardAsync(boardId, ct);
        return list.Select(p => new PermissionDto
        {
            UserId = p.UserId,
            BoardId = p.BoardId,
            Role = p.Role,
            GrantedAt = p.GrantedAt
        });
    }

    public async Task<IEnumerable<PermissionDto>> GetByUserAsync(Guid actorId, Guid userId, CancellationToken ct)
    {
        if (actorId != userId)
            throw new ForbiddenException("Cannot view another user's board permissions.");

        var list = await _permissions.GetByUserAsync(userId, ct);
        return list.Select(p => new PermissionDto
        {
            UserId = p.UserId,
            BoardId = p.BoardId,
            Role = p.Role,
            GrantedAt = p.GrantedAt
        });
    }

    public async Task<Guid> AddAsync(Guid actorId, Guid boardId, GrantPermissionDto dto, CancellationToken ct)
    {
        var board = await GetBoardOrThrow(boardId, ct);
        EnsureOwner(actorId, board.OwnerId);
        ValidateAssignable(dto.Role);

        if (dto.UserId == board.OwnerId)
            throw new ValidationException("Board owner already has full permissions.");

        var existing = await _permissions.GetAsync(boardId, dto.UserId, ct);
        if (existing is not null)
        {
            existing.Role = dto.Role;
            await _permissions.UpdateAsync(existing, ct);
            return boardId;
        }

        var p = new Permission
        {
            BoardId = boardId,
            UserId = dto.UserId,
            Role = dto.Role,
            GrantedAt = DateTime.UtcNow
        };

        return await _permissions.CreateAsync(p, ct);
    }

    public async Task<bool> UpdateAsync(Guid actorId, Guid boardId, Guid userId, UpdatePermissionDto dto, CancellationToken ct)
    {
        var board = await GetBoardOrThrow(boardId, ct);
        EnsureOwner(actorId, board.OwnerId);
        ValidateAssignable(dto.Role);

        if (userId == board.OwnerId)
            throw new ValidationException("Cannot modify the board owner's role.");

        var existing = await _permissions.GetAsync(boardId, userId, ct)
                       ?? throw new NotFoundException("Permission not found.");

        existing.Role = dto.Role;
        return await _permissions.UpdateAsync(existing, ct);
    }

    public async Task<bool> RemoveAsync(Guid actorId, Guid boardId, Guid userId, CancellationToken ct)
    {
        var board = await GetBoardOrThrow(boardId, ct);
        EnsureOwner(actorId, board.OwnerId);

        if (userId == board.OwnerId)
            throw new ValidationException("Cannot remove board owner.");

        return await _permissions.DeleteAsync(boardId, userId, ct);
    }
}
