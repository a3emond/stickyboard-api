using StickyBoard.Api.DTOs.Automation;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Clustering;
using StickyBoard.Api.Models.Enums;
using System.Text.Json;

namespace StickyBoard.Api.Services
{
    public sealed class RuleService
    {
        private readonly RuleRepository _rules;
        private readonly BoardRepository _boards;
        private readonly PermissionRepository _permissions;

        public RuleService(RuleRepository rules, BoardRepository boards, PermissionRepository permissions)
        {
            _rules = rules;
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
                throw new UnauthorizedAccessException("User not allowed to modify rules for this board.");
        }

        // ----------------------------------------------------------------------
        // READ
        // ----------------------------------------------------------------------

        public async Task<IEnumerable<RuleDto>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            await EnsureCanEditAsync(userId, boardId, ct);
            var list = await _rules.GetByBoardAsync(boardId, ct);
            return list.Select(Map);
        }

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public async Task<Guid> CreateAsync(Guid userId, Guid boardId, CreateRuleDto dto, CancellationToken ct)
        {
            await EnsureCanEditAsync(userId, boardId, ct);

            var rule = new Rule
            {
                BoardId = boardId,
                Definition = JsonDocument.Parse(dto.DefinitionJson ?? "{}"),
                Enabled = dto.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _rules.CreateAsync(rule, ct);
        }

        public async Task<bool> UpdateAsync(Guid userId, Guid ruleId, UpdateRuleDto dto, CancellationToken ct)
        {
            var existing = await _rules.GetByIdAsync(ruleId, ct);
            if (existing is null)
                return false;

            await EnsureCanEditAsync(userId, existing.BoardId, ct);

            if (dto.DefinitionJson is not null)
                existing.Definition = JsonDocument.Parse(dto.DefinitionJson);
            existing.Enabled = dto.Enabled ?? existing.Enabled;
            existing.UpdatedAt = DateTime.UtcNow;

            return await _rules.UpdateAsync(existing, ct);
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid ruleId, CancellationToken ct)
        {
            var existing = await _rules.GetByIdAsync(ruleId, ct);
            if (existing is null)
                return false;

            await EnsureCanEditAsync(userId, existing.BoardId, ct);
            return await _rules.DeleteAsync(ruleId, ct);
        }
        
        private static RuleDto Map(Rule r) => new()
        {
            Id = r.Id,
            BoardId = r.BoardId,
            DefinitionJson = r.Definition?.RootElement.GetRawText() ?? "{}",
            Enabled = r.Enabled,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}
