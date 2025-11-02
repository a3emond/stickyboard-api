using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Repositories.BoardsAndCards;

namespace StickyBoard.Api.Services;

public sealed class BoardMessageService
{
    private readonly BoardMessageRepository _repo;
    private readonly PermissionRepository _perm;

    public BoardMessageService(BoardMessageRepository repo, PermissionRepository perm)
    {
        _repo = repo;
        _perm = perm;
    }

    public async Task<IEnumerable<BoardMessageDto>> GetForBoardAsync(Guid actorId, Guid boardId, CancellationToken ct)
    {
        var perm = await _perm.GetAsync(boardId, actorId, ct);
        if (perm is null)
            throw new ForbiddenException("Not allowed.");

        var messages = await _repo.GetByBoardAsync(boardId, ct);

        return messages.Select(m => new BoardMessageDto
        {
            Id = m.Id,
            BoardId = m.BoardId,
            User = new UserDto { Id = m.UserId },
            Content = m.Content,
            CreatedAt = m.CreatedAt
        });
    }

    public async Task<Guid> CreateAsync(Guid actorId, Guid boardId, BoardMessageCreateDto dto, CancellationToken ct)
    {
        var perm = await _perm.GetAsync(boardId, actorId, ct);
        if (perm is null)
            throw new ForbiddenException("Not allowed.");

        var msg = new BoardMessage
        {
            BoardId = boardId,
            UserId = actorId,
            Content = dto.Content
        };

        return await _repo.CreateAsync(msg, ct);
    }
}