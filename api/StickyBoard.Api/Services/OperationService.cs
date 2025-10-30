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

        private async Task EnsureCanViewAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct)
                        ?? throw new KeyNotFoundException("Board not found.");

            var isOwner = board.OwnerId == userId;
            var role = (await _permissions.GetAsync(boardId, userId, ct))?.Role;

            if (!(isOwner || role is BoardRole.owner or BoardRole.editor or BoardRole.viewer))
                throw new UnauthorizedAccessException("User not allowed to access this board's operations.");
        }

        public async Task<Guid> AppendAsync(Guid userId, CreateOperationDto dto, CancellationToken ct)
        {
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
                DeviceId = dto.DeviceId,
                UserId = userId,
                Entity = dto.Entity,
                EntityId = dto.EntityId ?? throw new ArgumentException("EntityId is required."),
                OpType = dto.OpType,
                Payload = dto.PayloadJson is null
                    ? JsonDocument.Parse("{}")
                    : JsonSerializer.SerializeToDocument(dto.PayloadJson),
                VersionPrev = dto.VersionPrev,
                VersionNext = dto.VersionNext,
                CreatedAt = DateTime.UtcNow
            };

            return await _operations.CreateAsync(operation, ct);
        }

        public async Task<IEnumerable<OperationDto>> QueryAsync(Guid userId, OperationQueryDto dto, CancellationToken ct)
        {
            IEnumerable<Operation> ops;

            if (!string.IsNullOrWhiteSpace(dto.DeviceId))
            {
                if (Guid.TryParse(dto.DeviceId, out var devGuid))
                    ops = await _operations.GetByDeviceAsync(devGuid, ct);
                else
                    ops = [];
            }
            else if (dto.Since.HasValue)
                ops = await _operations.GetSinceAsync(dto.Since.Value, ct);
            else if (dto.UserId.HasValue)
                ops = await _operations.GetByUserAsync(dto.UserId.Value, ct);
            else
                ops = await _operations.GetByUserAsync(userId, ct);

            return ops.Select(Map);
        }

        public async Task<OperationMaintenanceResultDto> MaintenanceAsync(TimeSpan retention, CancellationToken ct)
        {
            var cutoff = DateTime.UtcNow.Subtract(retention);
            var deleted = await _operations.DeleteOlderThanAsync(cutoff, ct);

            return new OperationMaintenanceResultDto
            {
                DeletedCount = deleted,
                ProcessedCount = 0
            };
        }
        
        private static OperationDto Map(Operation o) => new()
        {
            Id = o.Id,
            DeviceId = o.DeviceId,
            UserId = o.UserId,
            Entity = o.Entity,
            EntityId = o.EntityId,
            OpType = o.OpType,
            PayloadJson = o.Payload?.Deserialize<Dictionary<string, object>>(),
            VersionPrev = o.VersionPrev,
            VersionNext = o.VersionNext,
            CreatedAt = o.CreatedAt,
            Processed = o.Processed
        };
    }
}
