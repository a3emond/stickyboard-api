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
        private readonly CardRepository _cards;
        private readonly PermissionRepository _permissions;

        public ActivityService(
            ActivityRepository activities,
            BoardRepository boards,
            CardRepository cards,
            PermissionRepository permissions)
        {
            _activities = activities;
            _boards = boards;
            _cards = cards;
            _permissions = permissions;
        }

        private async Task EnsureCanViewAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct)
                        ?? throw new KeyNotFoundException("Board not found.");

            var isOwner = board.OwnerId == userId;
            var role = (await _permissions.GetAsync(boardId, userId, ct))?.Role;

            if (!(isOwner || role is BoardRole.owner or BoardRole.editor or BoardRole.viewer))
                throw new UnauthorizedAccessException("User not allowed to view this board's activities.");
        }

        private static ActivityDto Map(Activity a) => new()
        {
            Id = a.Id,
            BoardId = a.BoardId,
            CardId = a.CardId,
            ActorId = a.ActorId,
            ActType = a.ActType,
            PayloadJson = a.Payload?.Deserialize<Dictionary<string, object>>() ?? new(),
            CreatedAt = a.CreatedAt
        };

        public async Task<IEnumerable<ActivityDto>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            await EnsureCanViewAsync(userId, boardId, ct);
            var entities = await _activities.GetByBoardAsync(boardId, ct);
            return entities.Select(Map);
        }

        public async Task<IEnumerable<ActivityDto>> GetByCardAsync(Guid userId, Guid cardId, CancellationToken ct)
        {
            var card = await _cards.GetByIdAsync(cardId, ct)
                        ?? throw new KeyNotFoundException("Card not found.");

            await EnsureCanViewAsync(userId, card.BoardId, ct);
            var entities = await _activities.GetByCardAsync(cardId, ct);
            return entities.Select(Map);
        }

        public async Task<IEnumerable<ActivityDto>> GetRecentAsync(Guid userId, int limit, CancellationToken ct)
        {
            var entities = await _activities.GetRecentAsync(limit, ct);
            return entities.Select(Map);
        }

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
                Payload = JsonSerializer.SerializeToDocument(dto.PayloadJson ?? new Dictionary<string, object>()),
                CreatedAt = DateTime.UtcNow
            };

            return await _activities.CreateAsync(entity, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            return await _activities.DeleteAsync(id, ct);
        }
    }
}
