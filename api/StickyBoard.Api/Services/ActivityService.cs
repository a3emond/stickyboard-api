using StickyBoard.Api.DTOs.Activities;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Activities;
using StickyBoard.Api.Models.Enums;
using System.Text.Json;

namespace StickyBoard.Api.Services
{
    public sealed class ActivityService
    {
        private readonly ActivityRepository _activities;
        private readonly BoardRepository _boards;
        private readonly PermissionRepository _permissions;

        public ActivityService(
            ActivityRepository activities,
            BoardRepository boards,
            PermissionRepository permissions)
        {
            _activities = activities;
            _boards = boards;
            _permissions = permissions;
        }

        private async Task EnsureCanViewAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct);
            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            var isOwner = board.OwnerId == userId;
            var role = (await _permissions.GetAsync(boardId, userId, ct))?.Role;

            if (!(isOwner || role is BoardRole.owner or BoardRole.editor or BoardRole.viewer))
                throw new UnauthorizedAccessException("User not allowed to view this board's activities.");
        }

        // ----------------------------------------------------------------------
        // READ
        // ----------------------------------------------------------------------

        public async Task<IEnumerable<Activity>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            await EnsureCanViewAsync(userId, boardId, ct);
            return await _activities.GetByBoardAsync(boardId, ct);
        }

        public async Task<IEnumerable<Activity>> GetByCardAsync(Guid userId, Guid cardId, CancellationToken ct)
        {
            // Retrieve card to get boardId (optional: skip if not needed)
            var activities = await _activities.GetByCardAsync(cardId, ct);
            return activities;
        }

        public async Task<IEnumerable<Activity>> GetRecentAsync(Guid userId, int limit, CancellationToken ct)
        {
            // For admin/dashboard — no board restriction
            return await _activities.GetRecentAsync(limit, ct);
        }

        // ----------------------------------------------------------------------
        // CREATE (append-only)
        // ----------------------------------------------------------------------

        public async Task<Guid> LogAsync(Guid actorId, CreateActivityDto dto, CancellationToken ct)
        {
            await EnsureCanViewAsync(actorId, dto.BoardId, ct);

            var entity = new Activity
            {
                Id = Guid.NewGuid(),
                BoardId = dto.BoardId,
                CardId = dto.CardId,
                ActorId = actorId,
                ActType = dto.ActType,
                Payload = JsonDocument.Parse(dto.PayloadJson ?? "{}"),
                CreatedAt = DateTime.UtcNow
            };

            return await _activities.CreateAsync(entity, ct);
        }

        // ----------------------------------------------------------------------
        // ADMIN (optional)
        // ----------------------------------------------------------------------

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            // For future admin cleanup
            return await _activities.DeleteAsync(id, ct);
        }
    }
}
