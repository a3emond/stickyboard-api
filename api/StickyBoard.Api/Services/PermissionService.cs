using StickyBoard.Api.DTOs.Permissions;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Boards;
namespace StickyBoard.Api.Services
{
    public sealed class PermissionService
    {
        private readonly PermissionRepository _permissions;
        private readonly BoardRepository _boards;

        public PermissionService(PermissionRepository permissions, BoardRepository boards)
        {
            _permissions = permissions;
            _boards = boards;
        }

        private async Task EnsureOwnerAsync(Guid actorId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct);
            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            if (board.OwnerId != actorId)
                throw new UnauthorizedAccessException("Only board owners can manage permissions.");
        }

        // ----------------------------------------------------------------------
        // READ
        // ----------------------------------------------------------------------

        public async Task<IEnumerable<PermissionDto>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var list = await _permissions.GetByBoardAsync(boardId, ct);
            return list.Select(Map);
        }

        public async Task<IEnumerable<PermissionDto>> GetByUserAsync(Guid userId, CancellationToken ct)
        {
            var list = await _permissions.GetByUserAsync(userId, ct);
            return list.Select(Map);
        }

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public async Task<Guid> AddAsync(Guid actorId, Guid boardId, GrantPermissionDto dto, CancellationToken ct)
        {
            await EnsureOwnerAsync(actorId, boardId, ct);

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
            await EnsureOwnerAsync(actorId, boardId, ct);

            var p = new Permission
            {
                BoardId = boardId,
                UserId = userId,
                Role = dto.Role
            };

            return await _permissions.UpdateAsync(p, ct);
        }

        public async Task<bool> RemoveAsync(Guid actorId, Guid boardId, Guid userId, CancellationToken ct)
        {
            await EnsureOwnerAsync(actorId, boardId, ct);
            return await _permissions.DeleteAsync(boardId, userId, ct);
        }
        
        private static PermissionDto Map(Permission p) => new()
        {
            UserId = p.UserId,
            BoardId = p.BoardId,
            Role = p.Role,
            GrantedAt = p.GrantedAt
        };
    }
}
