using StickyBoard.Api.DTOs.Sections;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.SectionsAndTabs;
using StickyBoard.Api.Models.Enums;
using System.Text.Json;

namespace StickyBoard.Api.Services
{
    public sealed class SectionService
    {
        private readonly SectionRepository _sections;
        private readonly BoardRepository _boards;
        private readonly PermissionRepository _permissions;

        public SectionService(SectionRepository sections, BoardRepository boards, PermissionRepository permissions)
        {
            _sections = sections;
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
                throw new UnauthorizedAccessException("User not allowed to modify this board.");
        }

        public async Task<IEnumerable<SectionDto>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct);
            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            var sections = await _sections.GetByBoardAsync(boardId, ct);
            return sections.Select(Map);
        }

        public async Task<Guid> CreateAsync(Guid userId, Guid boardId, CreateSectionDto dto, CancellationToken ct)
        {
            await EnsureCanEditAsync(userId, boardId, ct);

            var section = new Section
            {
                BoardId = boardId,
                Title = dto.Title,
                Position = dto.Position,
                LayoutMeta = JsonSerializer.SerializeToDocument(dto.LayoutMeta ?? new Dictionary<string, object>()),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _sections.CreateAsync(section, ct);
        }

        public async Task<bool> UpdateAsync(Guid userId, Guid sectionId, UpdateSectionDto dto, CancellationToken ct)
        {
            var current = await _sections.GetByIdAsync(sectionId, ct);
            if (current is null)
                return false;

            await EnsureCanEditAsync(userId, current.BoardId, ct);

            current.Title = dto.Title ?? current.Title;
            current.Position = dto.Position ?? current.Position;

            if (dto.LayoutMeta is not null)
                current.LayoutMeta = JsonSerializer.SerializeToDocument(dto.LayoutMeta);

            current.UpdatedAt = DateTime.UtcNow;

            return await _sections.UpdateAsync(current, ct);
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid sectionId, CancellationToken ct)
        {
            var current = await _sections.GetByIdAsync(sectionId, ct);
            if (current is null)
                return false;

            await EnsureCanEditAsync(userId, current.BoardId, ct);
            return await _sections.DeleteAsync(sectionId, ct);
        }

        public async Task<int> ReorderAsync(Guid userId, Guid boardId, IEnumerable<ReorderSectionDto> updates, CancellationToken ct)
        {
            await EnsureCanEditAsync(userId, boardId, ct);
            var mapped = updates.Select(u => (u.Id, u.Position));
            return await _sections.ReorderAsync(boardId, mapped, ct);
        }
        
        private static SectionDto Map(Section s) => new()
        {
            Id = s.Id,
            BoardId = s.BoardId,
            Title = s.Title,
            Position = s.Position,
            LayoutMeta = s.LayoutMeta.Deserialize<Dictionary<string, object>>()!,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
}
