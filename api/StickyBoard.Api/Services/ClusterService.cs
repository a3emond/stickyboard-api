using StickyBoard.Api.DTOs.Automation;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Clustering;
using StickyBoard.Api.Models.Enums;
using System.Text.Json;

namespace StickyBoard.Api.Services
{
    public sealed class ClusterService
    {
        private readonly ClusterRepository _clusters;
        private readonly BoardRepository _boards;
        private readonly PermissionRepository _permissions;

        public ClusterService(ClusterRepository clusters, BoardRepository boards, PermissionRepository permissions)
        {
            _clusters = clusters;
            _boards = boards;
            _permissions = permissions;
        }

        private async Task EnsureCanEditAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct);
            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            var isOwner = board.OwnerId == userId;
            var role = (await _permissions.GetAsync(boardId, userId, ct))?.Role;

            if (!(isOwner || role is BoardRole.owner or BoardRole.editor))
                throw new UnauthorizedAccessException("User not allowed to modify clusters for this board.");
        }

        public async Task<IEnumerable<ClusterDto>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            await EnsureCanEditAsync(userId, boardId, ct);
            var clusters = await _clusters.GetByBoardAsync(boardId, ct);
            return clusters.Select(Map);
        }

        public async Task<Guid> CreateAsync(Guid userId, Guid boardId, CreateClusterDto dto, CancellationToken ct)
        {
            await EnsureCanEditAsync(userId, boardId, ct);

            var cluster = new Cluster
            {
                BoardId = boardId,
                ClusterType = dto.ClusterType,
                RuleDef = dto.RuleDefJson is null ? null : JsonSerializer.SerializeToDocument(dto.RuleDefJson),
                VisualMeta = JsonSerializer.SerializeToDocument(dto.VisualMetaJson ?? new Dictionary<string, object>()),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _clusters.CreateAsync(cluster, ct);
        }

        public async Task<bool> UpdateAsync(Guid userId, Guid clusterId, UpdateClusterDto dto, CancellationToken ct)
        {
            var existing = await _clusters.GetByIdAsync(clusterId, ct);
            if (existing is null)
                return false;

            await EnsureCanEditAsync(userId, existing.BoardId, ct);

            if (dto.ClusterType.HasValue)
                existing.ClusterType = dto.ClusterType.Value;

            if (dto.RuleDefJson is not null)
                existing.RuleDef = JsonSerializer.SerializeToDocument(dto.RuleDefJson);
            if (dto.VisualMetaJson is not null)
                existing.VisualMeta = JsonSerializer.SerializeToDocument(dto.VisualMetaJson);

            existing.UpdatedAt = DateTime.UtcNow;
            return await _clusters.UpdateAsync(existing, ct);
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid clusterId, CancellationToken ct)
        {
            var existing = await _clusters.GetByIdAsync(clusterId, ct);
            if (existing is null)
                return false;

            await EnsureCanEditAsync(userId, existing.BoardId, ct);
            return await _clusters.DeleteAsync(clusterId, ct);
        }

        private static ClusterDto Map(Cluster c) => new()
        {
            Id = c.Id,
            BoardId = c.BoardId,
            ClusterType = c.ClusterType,
            RuleDefJson = c.RuleDef?.Deserialize<Dictionary<string, object>>(),
            VisualMetaJson = c.VisualMeta?.Deserialize<Dictionary<string, object>>() ?? new(),
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }
}
