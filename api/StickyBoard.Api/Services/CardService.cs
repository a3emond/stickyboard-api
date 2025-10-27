using StickyBoard.Api.DTOs.Cards;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Models.Cards;
using StickyBoard.Api.Models.Enums;
using System.Text.Json;

namespace StickyBoard.Api.Services
{
    public sealed class CardService
    {
        private readonly CardRepository _cards;
        private readonly BoardRepository _boards;
        private readonly PermissionRepository _permissions;

        public CardService(CardRepository cards, BoardRepository boards, PermissionRepository permissions)
        {
            _cards = cards;
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
                throw new UnauthorizedAccessException("User not allowed to modify cards on this board.");
        }

        // ----------------------------------------------------------------------
        // READ
        // ----------------------------------------------------------------------

        public Task<IEnumerable<Card>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
            => _cards.GetByBoardAsync(boardId, ct);

        public Task<IEnumerable<Card>> GetBySectionAsync(Guid userId, Guid sectionId, CancellationToken ct)
            => _cards.GetBySectionAsync(sectionId, ct);

        public Task<IEnumerable<Card>> GetByTabAsync(Guid userId, Guid tabId, CancellationToken ct)
            => _cards.GetByTabAsync(tabId, ct);

        public Task<IEnumerable<Card>> GetByAssigneeAsync(Guid userId, CancellationToken ct)
            => _cards.GetByAssigneeAsync(userId, ct);

        public Task<IEnumerable<Card>> SearchAsync(Guid userId, Guid boardId, string keyword, CancellationToken ct)
            => _cards.SearchAsync(boardId, keyword, ct);

        public Task<IEnumerable<Card>> GetByStatusAsync(Guid userId, Guid boardId, CardStatus status, CancellationToken ct)
            => _cards.GetByStatusAsync(boardId, status.ToString(), ct);

        // ----------------------------------------------------------------------
        // CREATE / UPDATE / DELETE
        // ----------------------------------------------------------------------

        public async Task<Guid> CreateAsync(Guid userId, CreateCardDto dto, CancellationToken ct)
        {
            await EnsureCanEditAsync(userId, dto.BoardId, ct);

            var entity = new Card
            {
                BoardId = dto.BoardId,
                SectionId = dto.SectionId,
                TabId = dto.TabId,
                Type = dto.Type,
                Title = dto.Title,
                Content = JsonDocument.Parse(dto.ContentJson ?? "{}"),
                InkData = string.IsNullOrWhiteSpace(dto.InkDataJson) ? null : JsonDocument.Parse(dto.InkDataJson),
                DueDate = dto.DueDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Priority = dto.Priority,
                Status = CardStatus.open,
                CreatedBy = userId,
                Version = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _cards.CreateAsync(entity, ct);
        }

        public async Task<bool> UpdateAsync(Guid userId, Guid cardId, UpdateCardDto dto, CancellationToken ct)
        {
            var card = await _cards.GetByIdAsync(cardId, ct);
            if (card is null)
                return false;

            await EnsureCanEditAsync(userId, card.BoardId, ct);

            card.SectionId = dto.SectionId ?? card.SectionId;
            card.TabId = dto.TabId ?? card.TabId;
            card.Title = dto.Title ?? card.Title;
            if (dto.ContentJson is not null)
                card.Content = JsonDocument.Parse(dto.ContentJson);
            if (dto.InkDataJson is not null)
                card.InkData = JsonDocument.Parse(dto.InkDataJson);
            card.DueDate = dto.DueDate ?? card.DueDate;
            card.StartTime = dto.StartTime ?? card.StartTime;
            card.EndTime = dto.EndTime ?? card.EndTime;
            card.Priority = dto.Priority ?? card.Priority;
            card.Status = dto.Status ?? card.Status;
            card.AssigneeId = dto.AssigneeId ?? card.AssigneeId;
            card.UpdatedAt = DateTime.UtcNow;

            return await _cards.UpdateAsync(card, ct);
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid cardId, CancellationToken ct)
        {
            var card = await _cards.GetByIdAsync(cardId, ct);
            if (card is null)
                return false;

            await EnsureCanEditAsync(userId, card.BoardId, ct);
            return await _cards.DeleteAsync(cardId, ct);
        }

        // ----------------------------------------------------------------------
        // BULK OPERATIONS
        // ----------------------------------------------------------------------

        public async Task<int> BulkAssignAsync(Guid userId, Guid boardId, Guid assigneeId, IEnumerable<Guid> cardIds, CancellationToken ct)
        {
            await EnsureCanEditAsync(userId, boardId, ct);
            return await _cards.BulkAssignUserAsync(boardId, assigneeId, cardIds, ct);
        }
    }
}
