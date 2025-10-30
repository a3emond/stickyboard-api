using StickyBoard.Api.DTOs.Boards;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Models.Boards;
using System.Text.Json;

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

                // Convert dictionaries → JsonDocument
                Theme = JsonSerializer.SerializeToDocument(dto.Theme ?? new Dictionary<string, object>()),
                Rules = JsonSerializer.SerializeToDocument(dto.Rules ?? new List<Dictionary<string, object>>()),

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

            // null means "don't touch", not "reset"
            if (dto.Theme != null)
                board.Theme = JsonSerializer.SerializeToDocument(dto.Theme);

            if (dto.Rules != null)
                board.Rules = JsonSerializer.SerializeToDocument(dto.Rules);

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

        public async Task<IEnumerable<BoardDto>> GetMineAsync(Guid ownerId, CancellationToken ct)
        {
            var boards = await _boards.GetByOwnerAsync(ownerId, ct);
            return boards.Select(Map);
        }

        public async Task<IEnumerable<BoardDto>> GetAccessibleAsync(Guid userId, CancellationToken ct)
        {
            var boards = await _boards.GetAccessibleForUserAsync(userId, ct);
            return boards.Select(Map);
        }

        private static BoardDto Map(Board b) => new()
        {
            Id = b.Id,
            OwnerId = b.OwnerId,
            OrganizationId = b.OrganizationId,
            ParentBoardId = b.ParentBoardId,
            Title = b.Title,
            Visibility = b.Visibility,
            
            // Convert JsonDocument → Dictionary/list
            Theme = b.Theme.Deserialize<Dictionary<string, object>>()!,
            Rules = b.Rules.Deserialize<List<Dictionary<string, object>>>()!,
            
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }
}
