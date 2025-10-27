using StickyBoard.Api.DTOs.Permissions;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Boards;
using StickyBoard.Api.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        public Task<IEnumerable<Permission>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
            => _permissions.GetByBoardAsync(boardId, ct);

        public Task<IEnumerable<Permission>> GetByUserAsync(Guid userId, CancellationToken ct)
            => _permissions.GetByUserAsync(userId, ct);

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
    }
}
