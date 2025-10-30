using StickyBoard.Api.DTOs.Tabs;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.SectionsAndTabs;
using StickyBoard.Api.Models.Enums;
using System.Text.Json;
using StickyBoard.Api.Repositories.SectionsAndTabs;

namespace StickyBoard.Api.Services
{
    public sealed class TabService
    {
        private readonly TabRepository _tabs;
        private readonly BoardRepository _boards;
        private readonly SectionRepository _sections;
        private readonly PermissionRepository _permissions;

        public TabService(TabRepository tabs, BoardRepository boards, SectionRepository sections, PermissionRepository permissions)
        {
            _tabs = tabs;
            _boards = boards;
            _sections = sections;
            _permissions = permissions;
        }

        private async Task EnsureCanEditAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct)
                        ?? throw new KeyNotFoundException("Board not found.");

            var isOwner = board.OwnerId == userId;
            var role = (await _permissions.GetAsync(boardId, userId, ct))?.Role;

            if (!(isOwner || role is BoardRole.owner or BoardRole.editor))
                throw new UnauthorizedAccessException("User not allowed to modify this board.");
        }

        private async Task EnsureCanViewAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct)
                        ?? throw new KeyNotFoundException("Board not found.");

            var isOwner = board.OwnerId == userId;
            var role = (await _permissions.GetAsync(boardId, userId, ct))?.Role;

            if (!(isOwner || role is BoardRole.owner or BoardRole.editor or BoardRole.viewer))
                throw new UnauthorizedAccessException("User not allowed to view this board.");
        }

        public async Task<IEnumerable<TabDto>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            await EnsureCanViewAsync(userId, boardId, ct);
            var tabs = await _tabs.GetByBoardAsync(boardId, ct);
            return tabs.Select(Map);
        }

        public async Task<IEnumerable<TabDto>> GetBySectionAsync(Guid userId, Guid sectionId, CancellationToken ct)
        {
            var section = await _sections.GetByIdAsync(sectionId, ct)
                          ?? throw new KeyNotFoundException("Section not found.");

            await EnsureCanViewAsync(userId, section.BoardId, ct);
            var tabs = await _tabs.GetBySectionAsync(sectionId, ct);
            return tabs.Select(Map);
        }

        public async Task<Guid> CreateAsync(Guid userId, Guid boardId, CreateTabDto dto, CancellationToken ct)
        {
            await EnsureCanEditAsync(userId, boardId, ct);

            if (dto.Scope == TabScope.section && dto.SectionId is null)
                throw new ArgumentException("SectionId required when scope = section.");

            var tab = new Tab
            {
                Scope = dto.Scope,
                BoardId = boardId,
                SectionId = dto.SectionId,
                Title = dto.Title,
                TabType = dto.TabType,
                LayoutConfig = JsonSerializer.SerializeToDocument(dto.LayoutConfig ?? new Dictionary<string, object>()),
                Position = dto.Position,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _tabs.CreateAsync(tab, ct);
        }

        public async Task<bool> UpdateAsync(Guid userId, Guid tabId, UpdateTabDto dto, CancellationToken ct)
        {
            var current = await _tabs.GetByIdAsync(tabId, ct);
            if (current is null)
                return false;

            await EnsureCanEditAsync(userId, current.BoardId, ct);

            current.Title = dto.Title ?? current.Title;
            current.TabType = dto.TabType ?? current.TabType;

            if (dto.LayoutConfig is not null)
                current.LayoutConfig = JsonSerializer.SerializeToDocument(dto.LayoutConfig);

            current.Position = dto.Position ?? current.Position;
            current.UpdatedAt = DateTime.UtcNow;

            return await _tabs.UpdateAsync(current, ct);
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid tabId, CancellationToken ct)
        {
            var current = await _tabs.GetByIdAsync(tabId, ct);
            if (current is null)
                return false;

            await EnsureCanEditAsync(userId, current.BoardId, ct);
            return await _tabs.DeleteAsync(tabId, ct);
        }

        public async Task<int> ReorderAsync(Guid userId, TabScope scope, Guid parentId, IEnumerable<ReorderTabDto> updates, CancellationToken ct)
        {
            if (scope == TabScope.board)
            {
                await EnsureCanEditAsync(userId, parentId, ct);
                return await _tabs.ReorderAsync(parentId, updates.Select(u => (u.Id, u.Position)), ct);
            }

            if (scope == TabScope.section)
            {
                var section = await _sections.GetByIdAsync(parentId, ct)
                              ?? throw new KeyNotFoundException("Section not found.");

                await EnsureCanEditAsync(userId, section.BoardId, ct);
                return await _tabs.ReorderAsync(parentId, updates.Select(u => (u.Id, u.Position)), ct);
            }

            throw new ArgumentOutOfRangeException(nameof(scope));
        }
        
        private static TabDto Map(Tab t) => new()
        {
            Id = t.Id,
            Scope = t.Scope,
            BoardId = t.BoardId,
            SectionId = t.SectionId,
            Title = t.Title,
            TabType = t.TabType,
            LayoutConfig = t.LayoutConfig.Deserialize<Dictionary<string, object>>()!,
            Position = t.Position,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}
