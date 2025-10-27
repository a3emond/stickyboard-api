using StickyBoard.Api.DTOs.Boards;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StickyBoard.Api.Models.Boards;

namespace StickyBoard.Api.Services
{
    public sealed class BoardService
    {
        private readonly BoardRepository _boards;
        private readonly PermissionRepository _permissions;

        public BoardService(BoardRepository boards, PermissionRepository permissions)
        {
            _boards = boards;
            _permissions = permissions;
        }

        public async Task<Guid> CreateAsync(Guid ownerId, CreateBoardDto dto, CancellationToken ct)
        {
            var board = new Board
            {
                OwnerId = ownerId,
                OrganizationId = dto.OrganizationId,
                ParentBoardId = dto.ParentBoardId,
                Title = dto.Title,
                Visibility = dto.Visibility,
                Theme = JsonDocument.Parse(dto.ThemeJson ?? "{}"),
                Rules = JsonDocument.Parse(dto.RulesJson ?? "[]"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _boards.CreateAsync(board, ct);
        }

        public async Task<bool> UpdateAsync(Guid actorId, Guid boardId, UpdateBoardDto dto, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct);
            if (board is null)
                return false;

            var canEdit = board.OwnerId == actorId ||
                (await _permissions.GetAsync(boardId, actorId, ct))?.Role is BoardRole.owner or BoardRole.editor;

            if (!canEdit)
                throw new UnauthorizedAccessException("User lacks permission to edit this board.");

            board.Title = dto.Title ?? board.Title;
            board.OrganizationId = dto.OrganizationId ?? board.OrganizationId;
            board.ParentBoardId = dto.ParentBoardId ?? board.ParentBoardId;
            board.Visibility = dto.Visibility ?? board.Visibility;
            if (dto.ThemeJson is not null)
                board.Theme = JsonDocument.Parse(dto.ThemeJson);
            if (dto.RulesJson is not null)
                board.Rules = JsonDocument.Parse(dto.RulesJson);

            board.UpdatedAt = DateTime.UtcNow;
            return await _boards.UpdateAsync(board, ct);
        }

        public async Task<bool> DeleteAsync(Guid actorId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct);
            if (board is null)
                return false;

            if (board.OwnerId != actorId)
                throw new UnauthorizedAccessException("Only owner can delete a board.");

            return await _boards.DeleteAsync(boardId, ct);
        }

        public Task<IEnumerable<Board>> GetMineAsync(Guid ownerId, CancellationToken ct)
            => _boards.GetByOwnerAsync(ownerId, ct);

        public Task<IEnumerable<Board>> GetAccessibleAsync(Guid userId, CancellationToken ct)
            => _boards.GetAccessibleForUserAsync(userId, ct);
    }
}
