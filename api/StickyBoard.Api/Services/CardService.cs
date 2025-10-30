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
                throw new UnauthorizedAccessException("No card edit permission.");
        }

        public async Task<IEnumerable<CardDto>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
            => (await _cards.GetByBoardAsync(boardId, ct)).Select(Map);

        public async Task<IEnumerable<CardDto>> GetBySectionAsync(Guid userId, Guid sectionId, CancellationToken ct)
            => (await _cards.GetBySectionAsync(sectionId, ct)).Select(Map);

        public async Task<IEnumerable<CardDto>> GetByTabAsync(Guid userId, Guid tabId, CancellationToken ct)
            => (await _cards.GetByTabAsync(tabId, ct)).Select(Map);

        public async Task<IEnumerable<CardDto>> GetByAssigneeAsync(Guid userId, CancellationToken ct)
            => (await _cards.GetByAssigneeAsync(userId, ct)).Select(Map);

        public async Task<IEnumerable<CardDto>> SearchAsync(Guid userId, Guid boardId, string keyword, CancellationToken ct)
            => (await _cards.SearchAsync(boardId, keyword, ct)).Select(Map);

        public async Task<IEnumerable<CardDto>> GetByStatusAsync(Guid userId, Guid boardId, CardStatus status, CancellationToken ct)
            => (await _cards.GetByStatusAsync(boardId, status.ToString(), ct)).Select(Map);

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
                Content = JsonSerializer.SerializeToDocument(dto.ContentJson ?? new Dictionary<string, object>()),
                InkData = dto.InkDataJson is null ? null : JsonSerializer.SerializeToDocument(dto.InkDataJson),
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
                card.Content = JsonSerializer.SerializeToDocument(dto.ContentJson);
            if (dto.InkDataJson is not null)
                card.InkData = JsonSerializer.SerializeToDocument(dto.InkDataJson);

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

        public async Task<int> BulkAssignAsync(Guid userId, Guid boardId, Guid assigneeId, IEnumerable<Guid> cardIds, CancellationToken ct)
        {
            await EnsureCanEditAsync(userId, boardId, ct);
            return await _cards.BulkAssignUserAsync(boardId, assigneeId, cardIds, ct);
        }

        private static CardDto Map(Card c) => new()
        {
            Id = c.Id,
            BoardId = c.BoardId,
            SectionId = c.SectionId,
            TabId = c.TabId,
            Type = c.Type,
            Title = c.Title,
            ContentJson = c.Content.Deserialize<Dictionary<string, object>>()!,
            InkDataJson = c.InkData?.Deserialize<Dictionary<string, object>>(),
            DueDate = c.DueDate,
            StartTime = c.StartTime,
            EndTime = c.EndTime,
            Priority = c.Priority,
            Status = c.Status,
            AssigneeId = c.AssigneeId,
            CreatedBy = c.CreatedBy,
            Version = c.Version,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }
}
