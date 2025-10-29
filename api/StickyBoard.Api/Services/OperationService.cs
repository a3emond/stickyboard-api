using StickyBoard.Api.DTOs.Operations;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Models.FilesAndOps;
using StickyBoard.Api.Repositories;
using System.Text.Json;
using StickyBoard.Api.Repositories.FilesAndOps;

namespace StickyBoard.Api.Services
{
    public sealed class OperationService
    {
        private readonly OperationRepository _operations;
        private readonly BoardRepository _boards;
        private readonly PermissionRepository _permissions;

        public OperationService(
            OperationRepository operations,
            BoardRepository boards,
            PermissionRepository permissions)
        {
            _operations = operations;
            _boards = boards;
            _permissions = permissions;
        }

        // ------------------------------------------------------------
        // PERMISSIONS
        // ------------------------------------------------------------
        private async Task EnsureCanViewAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct)
                        ?? throw new KeyNotFoundException("Board not found.");

            var isOwner = board.OwnerId == userId;
            var role = (await _permissions.GetAsync(boardId, userId, ct))?.Role;

            if (!(isOwner || role is BoardRole.owner or BoardRole.editor or BoardRole.viewer))
                throw new UnauthorizedAccessException("User not allowed to access this board's operations.");
        }

        // ------------------------------------------------------------
        // APPEND
        // ------------------------------------------------------------
        public async Task<Guid> AppendAsync(Guid userId, CreateOperationDto dto, CancellationToken ct)
        {
            // Optional: validate permission if entity is board-related
            if (dto.Entity is EntityType.board or EntityType.section or EntityType.card)
            {
                if (dto.EntityId.HasValue)
                {
                    var board = await _boards.GetByIdAsync(dto.EntityId.Value, ct);
                    if (board is not null)
                        await EnsureCanViewAsync(userId, board.Id, ct);
                }
            }

            var operation = new Operation
            {
                Id = Guid.NewGuid(),
                DeviceId = dto.DeviceId, // string type
                UserId = userId,
                Entity = dto.Entity,
                EntityId = dto.EntityId ?? throw new ArgumentException("EntityId is required."),
                OpType = dto.OpType,
                Payload = JsonDocument.Parse(dto.PayloadJson ?? "{}"),
                VersionPrev = dto.VersionPrev,
                VersionNext = dto.VersionNext,
                CreatedAt = DateTime.UtcNow
            };

            return await _operations.CreateAsync(operation, ct);
        }

        // ------------------------------------------------------------
        // QUERY
        // ------------------------------------------------------------
        public async Task<IEnumerable<Operation>> QueryAsync(Guid userId, OperationQueryDto dto, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(dto.DeviceId))
                return await _operations.GetByDeviceAsync(Guid.TryParse(dto.DeviceId, out var devGuid) ? devGuid : Guid.Empty, ct);

            if (dto.Since.HasValue)
                return await _operations.GetSinceAsync(dto.Since.Value, ct);

            if (dto.UserId.HasValue)
                return await _operations.GetByUserAsync(dto.UserId.Value, ct);

            // Default fallback: current user's recent operations
            return await _operations.GetByUserAsync(userId, ct);
        }

        // ------------------------------------------------------------
        // MAINTENANCE
        // ------------------------------------------------------------
        public async Task<OperationMaintenanceResultDto> MaintenanceAsync(TimeSpan retention, CancellationToken ct)
        {
            var cutoff = DateTime.UtcNow.Subtract(retention);
            var deleted = await _operations.DeleteOlderThanAsync(cutoff, ct);

            // Extend later: mark processed, archive, etc.
            return new OperationMaintenanceResultDto
            {
                DeletedCount = deleted,
                ProcessedCount = 0
            };
        }
    }
}
